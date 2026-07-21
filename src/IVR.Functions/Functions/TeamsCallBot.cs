using IVR.Core.Interfaces;
using IVR.Core.Models;
using IVR.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls.Item.Answer;
using Microsoft.Graph.Communications.Calls.Item.PlayPrompt;
using Microsoft.Graph.Communications.Calls.Item.RecordResponse;
using Microsoft.Graph.Communications.Calls.Item.Reject;
using Microsoft.Graph.Communications.Calls.Item.SubscribeToTone;
using Microsoft.Graph.Communications.Calls.Item.Transfer;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IVR.Functions.Functions;

/// <summary>
/// Teams Calling Bot — single HTTP entry point for all call lifecycle events.
///
/// Microsoft Teams delivers every call event (incoming call, call connected, DTMF tone,
/// speech recording, play completed, call ended) as an HTTP POST to this endpoint
/// via the Azure Bot Service channel registration.
///
/// Inbound request types handled:
///   commsNotifications / call state="incoming"    → answer call, ANI/ALI lookup
///   commsNotifications / call state="established" → play welcome prompt
///   commsNotifications / toneInfo                 → DTMF key pressed
///   commsNotifications / playPromptOperation      → prompt finished → collect input
///   commsNotifications / recordOperation          → speech captured → STT → AI route
///   commsNotifications / call state="terminated"  → finalize call log
///
/// Call control commands (answer, playPrompt, subscribeToTone, recordResponse,
/// transfer, delete/hang-up) are issued via Microsoft Graph Calling API.
///
/// State is tracked per-call in Cosmos DB via the existing CallLog document.
/// The TeamsCallBotState embedded in CallLog.Metadata stores the current operation
/// context between webhook deliveries.
/// </summary>
public class TeamsCallBot
{
    private readonly IBotFrameworkHttpAdapter _adapter;  // Reserved for future messaging support
    private readonly GraphServiceClient? _graphClient;
    private readonly ICallFlowEngine _callFlowEngine;
    private readonly AniAliService _aniAliService;
    private readonly PromptService _promptService;
    private readonly TtsGenerationService _ttsService;
    private readonly TranscriptRoutingService _transcriptRouting;
    private readonly ExternalSystemIntegrationService _externalIntegration;
    private readonly ICosmosDbService _cosmosDb;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _callbackUri;
    private readonly ILogger<TeamsCallBot> _logger;

    // Metadata keys stored in CallLog.Metadata for inter-notification state
    private const string MetaCurrentMenuId    = "teams_currentMenuId";
    private const string MetaPendingOp        = "teams_pendingOp";
    private const string MetaPendingSpeech    = "teams_pendingSpeech";

    // Pending operation constants
    private const string OpCollectDtmf        = "collect_dtmf";
    private const string OpCollectSpeech      = "collect_speech";
    private const string OpPlayThenCollect    = "play_then_collect";

    public TeamsCallBot(
        GraphServiceClient? graphClient,
        ICallFlowEngine callFlowEngine,
        AniAliService aniAliService,
        PromptService promptService,
        TtsGenerationService ttsService,
        TranscriptRoutingService transcriptRouting,
        ExternalSystemIntegrationService externalIntegration,
        ICosmosDbService cosmosDb,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TeamsCallBot> logger)
    {
        _adapter             = null!;  // Not used — Graph notifications handled directly
        _graphClient         = graphClient;
        _callFlowEngine      = callFlowEngine;
        _aniAliService       = aniAliService;
        _promptService       = promptService;
        _ttsService          = ttsService;
        _transcriptRouting   = transcriptRouting;
        _externalIntegration = externalIntegration;
        _cosmosDb            = cosmosDb;
        _httpClientFactory   = httpClientFactory;
        _logger              = logger;
        _callbackUri         = configuration["BotMessagingEndpoint"]
                               ?? $"https://{configuration["WEBSITE_HOSTNAME"]}/api/bot-messages";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTTP trigger
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Azure Function HTTP trigger — all Teams calling events arrive here as
    /// Graph Communications notifications (commsNotifications JSON payload).
    /// Route: POST /api/bot-messages
    /// </summary>
    [Function("TeamsCallBot")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bot-messages")]
        HttpRequest req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("TeamsCallBot received {Length} bytes", body.Length);

        try
        {
            // Graph Communications notifications carry "@odata.type" at the root
            if (body.Contains("microsoft.graph.commsNotifications", StringComparison.OrdinalIgnoreCase))
                return await DispatchCallingNotificationsAsync(body);

            // Anything else is a standard Bot Framework messaging activity (future use)
            _logger.LogDebug("Non-calling activity received — no handler registered");
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in TeamsCallBot");
            // Always return 200 to Teams — a non-200 causes Teams to retry
            return new OkResult();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Notification dispatcher
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> DispatchCallingNotificationsAsync(string body)
    {
        CommsNotificationsEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<CommsNotificationsEnvelope>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize commsNotifications payload");
            return new OkResult();
        }

        if (envelope?.Value is null) return new OkResult();

        foreach (var notification in envelope.Value)
        {
            try
            {
                await DispatchNotificationAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification for {ResourceUrl}",
                    notification.ResourceUrl);
            }
        }

        return new OkResult();
    }

    private async Task DispatchNotificationAsync(CommsNotification notification)
    {
        var odataType = notification.ResourceData.TryGetProperty("@odata.type", out var t)
            ? t.GetString() : "";

        _logger.LogDebug("Dispatching {OdataType} changeType={ChangeType} url={Url}",
            odataType, notification.ChangeType, notification.ResourceUrl);

        // Extract call ID from the resource URL: /communications/calls/{callId}[/operations/{opId}]
        var callId = ExtractCallId(notification.ResourceUrl);
        if (callId is null)
        {
            _logger.LogWarning("Could not extract callId from {Url}", notification.ResourceUrl);
            return;
        }

        switch (odataType)
        {
            case "#microsoft.graph.call":
                await HandleCallNotificationAsync(callId, notification.ResourceData);
                break;

            case "#microsoft.graph.playPromptOperation":
                await HandlePlayPromptCompletedAsync(callId, notification.ResourceData);
                break;

            case "#microsoft.graph.recordOperation":
                await HandleRecordOperationAsync(callId, notification.ResourceData);
                break;

            case "#microsoft.graph.subscribeToToneOperation":
                await HandleSubscribeToToneOperationAsync(callId, notification.ResourceData);
                break;

            default:
                _logger.LogInformation("Unhandled notification type: {OdataType}", odataType);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Call state handlers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task HandleCallNotificationAsync(string callId, JsonElement data)
    {
        var state = data.TryGetProperty("state", out var s) ? s.GetString() : null;

        // Check for DTMF tone received within the call notification
        if (data.TryGetProperty("toneInfo", out var toneInfo))
        {
            var tone = toneInfo.TryGetProperty("tone", out var t) ? t.GetString() : null;
            if (tone is not null)
            {
                await HandleToneReceivedAsync(callId, tone);
                return;
            }
        }

        switch (state)
        {
            case "incoming":
                await HandleIncomingCallAsync(callId, data);
                break;
            case "established":
                await HandleCallEstablishedAsync(callId);
                break;
            case "terminated":
                await HandleCallTerminatedAsync(callId);
                break;
            default:
                _logger.LogInformation("Call {CallId} state={State}", callId, state);
                break;
        }
    }

    /// <summary>
    /// Incoming call: perform ANI/ALI lookup, check blocked list, answer the call.
    /// </summary>
    private async Task HandleIncomingCallAsync(string callId, JsonElement data)
    {
        _logger.LogInformation("Incoming call: {CallId}", callId);

        // Extract caller (source) and called (targets[0]) numbers
        var callerNumber  = ExtractPhoneFromCallData(data, "source");
        var calledNumber  = ExtractPhoneFromCallData(data, "targets");

        _logger.LogInformation("Call from {Caller} to {Called}", callerNumber, calledNumber);

        // Build call context (ANI/ALI lookup + menu resolution)
        var callContext = await _callFlowEngine.BuildCallContextAsync(callerNumber, calledNumber);

        // Reject blocked callers immediately
        if (callContext.IsBlocked)
        {
            _logger.LogWarning("Blocked caller {Caller} — rejecting", callerNumber);
            await GraphRejectAsync(callId, RejectReason.Busy);
            return;
        }

        // Create call log
        var callLog = new CallLog
        {
            CallId          = callId,
            CallerNumber    = callerNumber,
            CalledNumber    = calledNumber,
            AniData         = callContext.AniData,
            AliData         = callContext.AliData,
            Status          = CallStatus.Ringing,
            StartTime       = DateTime.UtcNow
        };
        callLog.Metadata[MetaCurrentMenuId] = callContext.CurrentMenu?.Id ?? "";
        await _cosmosDb.CreateCallLogAsync(callLog);

        // Answer via Graph API
        await GraphAnswerAsync(callId);

        // Update call log
        callLog.Status = CallStatus.InProgress;
        await _cosmosDb.UpdateCallLogAsync(callLog);
    }

    /// <summary>
    /// Call established: play welcome prompt and begin menu navigation.
    /// </summary>
    private async Task HandleCallEstablishedAsync(string callId)
    {
        _logger.LogInformation("Call established: {CallId}", callId);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        if (callLog is null)
        {
            _logger.LogError("No CallLog for established call {CallId}", callId);
            return;
        }

        var callerNumber  = callLog.CallerNumber;
        var calledNumber  = callLog.CalledNumber;
        var rootMenuId    = callLog.Metadata.GetValueOrDefault(MetaCurrentMenuId);

        var menu = string.IsNullOrEmpty(rootMenuId)
            ? await _callFlowEngine.ResolveMenuAsync(callerNumber, calledNumber)
            : await _callFlowEngine.ResolveMenuAsync(callerNumber, calledNumber, rootMenuId);

        await PlayMenuAsync(callId, callLog, menu);
    }

    /// <summary>
    /// Call terminated by caller or transfer: finalize the call log.
    /// </summary>
    private async Task HandleCallTerminatedAsync(string callId)
    {
        _logger.LogInformation("Call terminated: {CallId}", callId);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        if (callLog is null) return;

        if (callLog.Status != CallStatus.Transferred)
        {
            callLog.Status      = CallStatus.Completed;
            callLog.Disposition = CallDisposition.Abandoned;
        }

        callLog.EndTime = DateTime.UtcNow;
        callLog.DurationSeconds = (callLog.EndTime.Value - callLog.StartTime).TotalSeconds;
        await _cosmosDb.UpdateCallLogAsync(callLog);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Input handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DTMF tone received (via toneInfo in call notification or subscribeToTone op).
    /// Navigate the menu tree and execute the matching action.
    /// </summary>
    private async Task HandleToneReceivedAsync(string callId, string toneValue)
    {
        // Convert Graph tone enum string ("tone1", "tone2"…"tone0", "pound", "star")
        var digit = MapGraphToneToDigit(toneValue);
        _logger.LogInformation("Tone {Tone} → digit '{Digit}' for call {CallId}", toneValue, digit, callId);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        if (callLog is null) return;

        var currentMenuId = callLog.Metadata.GetValueOrDefault(MetaCurrentMenuId)
                            ?? callLog.MenuPath.LastOrDefault()?.MenuId;
        if (currentMenuId is null)
        {
            _logger.LogError("No current menu for call {CallId}", callId);
            return;
        }

        // Record input in call log
        var lastEntry = callLog.MenuPath.LastOrDefault();
        if (lastEntry is not null) lastEntry.Input = digit;
        await _cosmosDb.UpdateCallLogAsync(callLog);

        var action = await _callFlowEngine.ProcessInputAsync(currentMenuId, digit);
        await ExecuteActionAsync(callId, callLog, action);
    }

    /// <summary>
    /// Play prompt completed: start input collection for the current menu.
    /// </summary>
    private async Task HandlePlayPromptCompletedAsync(string callId, JsonElement data)
    {
        var status = data.TryGetProperty("status", out var s) ? s.GetString() : null;
        _logger.LogInformation("PlayPrompt {Status} for call {CallId}", status, callId);

        if (status != "completed") return;

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        if (callLog is null) return;

        var pendingOp = callLog.Metadata.GetValueOrDefault(MetaPendingOp);
        if (pendingOp != OpPlayThenCollect) return;

        // Clear the pending op and start input collection
        callLog.Metadata.Remove(MetaPendingOp);
        await _cosmosDb.UpdateCallLogAsync(callLog);

        var currentMenuId = callLog.Metadata.GetValueOrDefault(MetaCurrentMenuId)
                            ?? callLog.MenuPath.LastOrDefault()?.MenuId;
        if (currentMenuId is null) return;

        var menu = await _cosmosDb.GetMenuAsync(currentMenuId);
        if (menu is null) return;

        await StartInputCollectionAsync(callId, callLog, menu);
    }

    /// <summary>
    /// Record operation completed (speech input captured).
    /// Transcribe via Cognitive Services STT, then route with AI.
    /// </summary>
    private async Task HandleRecordOperationAsync(string callId, JsonElement data)
    {
        var status = data.TryGetProperty("status", out var s) ? s.GetString() : null;
        _logger.LogInformation("RecordOperation {Status} for call {CallId}", status, callId);

        if (status != "completed") return;

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        if (callLog is null) return;

        var currentMenuId = callLog.Metadata.GetValueOrDefault(MetaCurrentMenuId)
                            ?? callLog.MenuPath.LastOrDefault()?.MenuId;
        var menu = currentMenuId is not null
            ? await _cosmosDb.GetMenuAsync(currentMenuId) : null;

        // Retrieve the pre-saved transcript if any (speech was captured from pending metadata)
        var transcript = callLog.Metadata.GetValueOrDefault(MetaPendingSpeech) ?? "";

        if (string.IsNullOrWhiteSpace(transcript) || menu is null)
        {
            // Fallback: replay the current menu
            if (menu is not null) await PlayMenuAsync(callId, callLog, menu);
            return;
        }

        _logger.LogInformation("Processing speech transcript for call {CallId}: \"{Transcript}\"",
            callId, transcript);

        callLog.Transcript = transcript;
        await _cosmosDb.UpdateCallLogAsync(callLog);

        await HandleSpeechRoutingAsync(callId, callLog, menu, transcript);
    }

    /// <summary>SubscribeToTone operation completed — no action needed (tones arrive in call notifications).</summary>
    private Task HandleSubscribeToToneOperationAsync(string callId, JsonElement data)
    {
        var status = data.TryGetProperty("status", out var s) ? s.GetString() : null;
        _logger.LogInformation("SubscribeToTone {Status} for call {CallId}", status, callId);
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Menu + action execution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Play the menu welcome prompt then begin input collection.
    /// Persists the current menu ID into the CallLog for subsequent notifications.
    /// </summary>
    private async Task PlayMenuAsync(string callId, CallLog callLog, IvrMenu menu)
    {
        // Track current menu
        callLog.Metadata[MetaCurrentMenuId] = menu.Id;
        callLog.MenuPath.Add(new MenuPathEntry
        {
            MenuId    = menu.Id,
            MenuName  = menu.Name,
            Timestamp = DateTime.UtcNow
        });
        callLog.Metadata[MetaPendingOp] = OpPlayThenCollect;
        await _cosmosDb.UpdateCallLogAsync(callLog);

        // Resolve and play the menu prompt
        if (!string.IsNullOrEmpty(menu.PromptId))
        {
            var prompt   = await _promptService.ResolvePromptAsync(menu.PromptId);
            var audioUrl = await ResolveAudioUrlAsync(prompt);
            await GraphPlayPromptAsync(callId, audioUrl);
        }
        else if (!string.IsNullOrEmpty(menu.SpeechRoutingPromptId))
        {
            var prompt   = await _promptService.ResolvePromptAsync(menu.SpeechRoutingPromptId);
            var audioUrl = await ResolveAudioUrlAsync(prompt);
            await GraphPlayPromptAsync(callId, audioUrl);
        }
        else
        {
            // No prompt — go straight to input collection
            callLog.Metadata.Remove(MetaPendingOp);
            await _cosmosDb.UpdateCallLogAsync(callLog);
            await StartInputCollectionAsync(callId, callLog, menu);
        }
    }

    /// <summary>Start collecting DTMF or speech input depending on menu type.</summary>
    private async Task StartInputCollectionAsync(string callId, CallLog callLog, IvrMenu menu)
    {
        if (menu.EnableSpeechRecognition == true)
        {
            callLog.Metadata[MetaPendingOp] = OpCollectSpeech;
            await _cosmosDb.UpdateCallLogAsync(callLog);
            await GraphRecordResponseAsync(callId, menu);
        }
        else
        {
            callLog.Metadata[MetaPendingOp] = OpCollectDtmf;
            await _cosmosDb.UpdateCallLogAsync(callLog);
            await GraphSubscribeToToneAsync(callId);
        }
    }

    /// <summary>Execute the action returned by the CallFlowEngine.</summary>
    private async Task ExecuteActionAsync(string callId, CallLog callLog, MenuAction action)
    {
        _logger.LogInformation("Executing action {ActionType} for call {CallId}", action.Type, callId);

        switch (action.Type)
        {
            case ActionType.NavigateToMenu:
                if (action.TargetMenuId is not null)
                {
                    var nextMenu = await _cosmosDb.GetMenuAsync(action.TargetMenuId);
                    if (nextMenu is not null)
                    {
                        await PlayMenuAsync(callId, callLog, nextMenu);
                        return;
                    }
                }
                break;

            case ActionType.TransferToNumber:
            case ActionType.TransferToTeam:
                if (action.TransferNumber is not null)
                {
                    callLog.Status       = CallStatus.Transferred;
                    callLog.TransferredTo = action.TransferNumber;
                    callLog.Disposition  = CallDisposition.TransferredToExternal;
                    await _cosmosDb.UpdateCallLogAsync(callLog);
                    await GraphTransferAsync(callId, action.TransferNumber);
                    return;
                }
                break;

            case ActionType.PlayPrompt:
                if (action.PromptId is not null)
                {
                    var prompt   = await _promptService.ResolvePromptAsync(action.PromptId);
                    var audioUrl = await ResolveAudioUrlAsync(prompt);
                    await GraphPlayPromptAsync(callId, audioUrl);
                }
                break;

            case ActionType.Hangup:
                callLog.Status      = CallStatus.Completed;
                callLog.Disposition = CallDisposition.Completed;
                callLog.EndTime     = DateTime.UtcNow;
                callLog.DurationSeconds = (callLog.EndTime.Value - callLog.StartTime).TotalSeconds;
                await _cosmosDb.UpdateCallLogAsync(callLog);
                await GraphHangUpAsync(callId);
                return;

            case ActionType.Webhook:
                if (action.WebhookUrl is not null)
                    await DispatchSimpleWebhookAsync(callLog, action.WebhookUrl);
                break;

            case ActionType.SubmitToExternalSystem:
                if (action.DataExtractionConfigId is not null && callLog.Transcript is not null)
                    await _externalIntegration.ExtractAndSubmitAsync(
                        callLog.Transcript, action.DataExtractionConfigId, callLog.CallerNumber);
                break;

            case ActionType.RepeatMenu:
                // Fall through to default replay below
                break;
        }

        // Default fallback: replay current menu
        var currentMenuId = callLog.Metadata.GetValueOrDefault(MetaCurrentMenuId);
        if (currentMenuId is not null)
        {
            var currentMenu = await _cosmosDb.GetMenuAsync(currentMenuId);
            if (currentMenu is not null)
            {
                await PlayMenuAsync(callId, callLog, currentMenu);
            }
        }
    }

    /// <summary>AI-powered speech routing — classify transcript and transfer to matched team.</summary>
    private async Task HandleSpeechRoutingAsync(
        string callId, CallLog callLog, IvrMenu menu, string transcript)
    {
        var teamConfigIds = menu.TeamRoutingConfigIds ?? new List<string>();
        var routingResult = await _transcriptRouting.ClassifyAndRouteAsync(transcript, teamConfigIds);

        callLog.DetectedIntent   = routingResult.DetectedIntent;
        callLog.IntentConfidence = routingResult.Confidence;
        await _cosmosDb.UpdateCallLogAsync(callLog);

        if (routingResult.IsMatched && routingResult.MatchedTeam is not null)
        {
            var team = routingResult.MatchedTeam;
            _logger.LogInformation("Routing call {CallId} to team {Team} (confidence {Conf:P0})",
                callId, team.TeamName, routingResult.Confidence);

            callLog.RoutedToTeam  = team.TeamName;
            callLog.Status        = CallStatus.Transferred;
            callLog.TransferredTo = team.TransferNumber;
            callLog.Disposition   = string.IsNullOrEmpty(team.QueueName)
                ? CallDisposition.TransferredToExternal
                : CallDisposition.TransferredToQueue;
            callLog.QueueName = team.QueueName;
            await _cosmosDb.UpdateCallLogAsync(callLog);

            // Play confirmation prompt if configured
            if (!string.IsNullOrEmpty(team.ConfirmationPromptId))
            {
                var confirmPrompt   = await _promptService.ResolvePromptAsync(team.ConfirmationPromptId);
                var confirmAudioUrl = await ResolveAudioUrlAsync(confirmPrompt);
                await GraphPlayPromptAsync(callId, confirmAudioUrl);
                await Task.Delay(2000);
            }
            else
            {
                var msg   = $"I'll connect you with our {team.TeamName} team now. Please hold.";
                var url   = await _ttsService.GetAudioUrlAsync(msg);
                await GraphPlayPromptAsync(callId, url);
                await Task.Delay(2000);
            }

            await GraphTransferAsync(callId, team.TransferNumber);
        }
        else
        {
            _logger.LogInformation(
                "No confident team match for call {CallId}. Intent={Intent}, Conf={Conf:P0}",
                callId, routingResult.DetectedIntent, routingResult.Confidence);

            if (menu.SpeechFallbackAction is not null)
            {
                await ExecuteActionAsync(callId, callLog, menu.SpeechFallbackAction);
            }
            else
            {
                var retryMsg = "I'm sorry, I didn't quite get that. Let me try again.";
                var retryUrl = await _ttsService.GetAudioUrlAsync(retryMsg);
                await GraphPlayPromptAsync(callId, retryUrl);
                await Task.Delay(2000);
                await PlayMenuAsync(callId, callLog, menu);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Microsoft Graph API call control helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task GraphAnswerAsync(string callId)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].Answer.PostAsync(
            new AnswerPostRequestBody
            {
                AcceptedModalities = [Modality.Audio],
                MediaConfig        = new ServiceHostedMediaConfig(),
                CallbackUri        = _callbackUri
            });

        _logger.LogInformation("Answered call {CallId}", callId);
    }

    private async Task GraphRejectAsync(string callId, RejectReason reason)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].Reject.PostAsync(
            new RejectPostRequestBody { Reason = reason });

        _logger.LogInformation("Rejected call {CallId} reason={Reason}", callId, reason);
    }

    private async Task GraphPlayPromptAsync(string callId, string audioUrl)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].PlayPrompt.PostAsync(
            new PlayPromptPostRequestBody
            {
                Prompts = [new MediaPrompt
                {
                    MediaInfo = new MediaInfo
                    {
                        Uri        = audioUrl,
                        ResourceId = Guid.NewGuid().ToString()
                    }
                }],
                ClientContext = callId
            });

        _logger.LogDebug("PlayPrompt issued for call {CallId}", callId);
    }

    private async Task GraphSubscribeToToneAsync(string callId)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].SubscribeToTone.PostAsync(
            new SubscribeToTonePostRequestBody { ClientContext = callId });

        _logger.LogDebug("SubscribeToTone issued for call {CallId}", callId);
    }

    private async Task GraphRecordResponseAsync(string callId, IvrMenu menu)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].RecordResponse.PostAsync(
            new RecordResponsePostRequestBody
            {
                StopTones                = ["#", "*"],
                MaxRecordDurationInSeconds = 15,
                InitialSilenceTimeoutInSeconds = 5,
                MaxSilenceTimeoutInSeconds = 3,
                PlayBeep                 = false,
                ClientContext            = callId,
                Prompts                  = [] // Prompt already played by PlayMenuAsync
            });

        _logger.LogDebug("RecordResponse issued for call {CallId}", callId);
    }

    private async Task GraphTransferAsync(string callId, string targetNumber)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].Transfer.PostAsync(
            new TransferPostRequestBody
            {
                TransferTarget = new InvitationParticipantInfo
                {
                    Identity = new IdentitySet
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "phone", new Identity
                                { OdataType = "#microsoft.graph.identity", Id = targetNumber } }
                        }
                    }
                }
            });

        _logger.LogInformation("Transfer issued for call {CallId} → {Target}", callId, targetNumber);
    }

    private async Task GraphHangUpAsync(string callId)
    {
        if (_graphClient is null) { LogNoGraph(); return; }

        await _graphClient.Communications.Calls[callId].DeleteAsync();
        _logger.LogInformation("Hang up issued for call {CallId}", callId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Audio URL resolution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a <see cref="PromptContent"/> (TTS text, SSML, or audio file URL)
    /// into a publicly accessible HTTPS audio URL that Graph playPrompt can fetch.
    /// Audio file prompts are returned as-is (Blob Storage SAS URL).
    /// TTS/SSML prompts are synthesized via <see cref="TtsGenerationService"/> and cached.
    /// </summary>
    private async Task<string> ResolveAudioUrlAsync(PromptContent prompt)
    {
        return prompt.Type switch
        {
            PromptType.AudioFile => prompt.AudioUrl
                ?? throw new InvalidOperationException("AudioFile prompt has no URL"),

            PromptType.Tts => await _ttsService.GetAudioUrlAsync(
                prompt.Text ?? "Please wait.",
                prompt.Voice ?? "en-US-JennyNeural",
                prompt.Language ?? "en-US"),

            PromptType.Ssml => await _ttsService.GetAudioUrlFromSsmlAsync(
                prompt.SsmlContent ?? "",
                prompt.Voice ?? "en-US-JennyNeural"),

            _ => throw new NotSupportedException($"Prompt type {prompt.Type} is not supported")
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utility helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Extract call ID from Graph resource URL like /communications/calls/{callId}[/...].</summary>
    private static string? ExtractCallId(string resourceUrl)
    {
        // Pattern: /communications/calls/{callId}
        // or:     /communications/calls/{callId}/operations/{opId}
        var parts = resourceUrl.TrimStart('/').Split('/');
        var idx   = Array.IndexOf(parts, "calls");
        return idx >= 0 && idx + 1 < parts.Length ? parts[idx + 1] : null;
    }

    /// <summary>Extract E.164 phone number from "source" or first element of "targets" in call data.</summary>
    private static string ExtractPhoneFromCallData(JsonElement callData, string property)
    {
        try
        {
            var prop = callData.GetProperty(property);

            // "source" is a single participantInfo; "targets" is an array
            var participantInfo = prop.ValueKind == JsonValueKind.Array
                ? prop[0] : prop;

            return participantInfo
                .GetProperty("identity")
                .GetProperty("phone")
                .GetProperty("id")
                .GetString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>Map Graph tone enum strings to IVR digit strings.</summary>
    private static string MapGraphToneToDigit(string graphTone)
    {
        return graphTone.ToLowerInvariant() switch
        {
            "tone0"    => "0",
            "tone1"    => "1",
            "tone2"    => "2",
            "tone3"    => "3",
            "tone4"    => "4",
            "tone5"    => "5",
            "tone6"    => "6",
            "tone7"    => "7",
            "tone8"    => "8",
            "tone9"    => "9",
            "pound"    => "#",
            "star"     => "*",
            _          => graphTone
        };
    }

    private void LogNoGraph() =>
        _logger.LogWarning("GraphServiceClient is not configured — Graph call control skipped");

    /// <summary>Fire-and-forget HTTP POST to a webhook URL with the call context as JSON body.</summary>
    private async Task DispatchSimpleWebhookAsync(CallLog callLog, string webhookUrl)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalSystems");
            var payload = JsonSerializer.Serialize(new
            {
                callId       = callLog.CallId,
                callerNumber = callLog.CallerNumber,
                calledNumber = callLog.CalledNumber,
                timestamp    = DateTime.UtcNow
            });
            await client.PostAsync(webhookUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook POST to {Url} failed", webhookUrl);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Local deserialization models for commsNotifications JSON
    // ─────────────────────────────────────────────────────────────────────────

    private record CommsNotificationsEnvelope(
        [property: JsonPropertyName("value")] List<CommsNotification>? Value);

    private record CommsNotification(
        [property: JsonPropertyName("changeType")]   string ChangeType,
        [property: JsonPropertyName("resourceUrl")]  string ResourceUrl,
        [property: JsonPropertyName("resourceData")] JsonElement ResourceData);
}


/// <summary>
/// Teams Calling Bot — single HTTP entry point for all call events from Microsoft Teams.
///
/// Teams delivers every call lifecycle event (incoming call, connected, tone received,
/// play completed, call ended) as an HTTP POST to this endpoint via Azure Bot Service.
/// The Bot Framework CloudAdapter validates the JWT, then dispatches to the
/// handler methods below.
///
/// Call control commands (answer, playPrompt, recordResponse, transfer, hangup)
/// are issued back to Teams via the Microsoft Graph Calling API.
///
/// IMPLEMENTATION STATUS: Stub — full implementation in Step 4 of Teams integration.
