using Azure.Communication;
using Azure.Communication.CallAutomation;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using IVR.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace IVR.Functions.Functions;

/// <summary>
/// Handles call automation callbacks (play completed, DTMF received, etc.)
/// This drives the multi-level IVR menu navigation.
/// </summary>
public class CallbackHandler
{
    private readonly CallAutomationClient _callClient;
    private readonly ICallFlowEngine _callFlowEngine;
    private readonly PromptService _promptService;
    private readonly TranscriptRoutingService _transcriptRouting;
    private readonly ExternalSystemIntegrationService _externalIntegration;
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<CallbackHandler> _logger;
    private static readonly ConcurrentDictionary<string, string> _callerByConnection = new();

    public CallbackHandler(
        CallAutomationClient callClient,
        ICallFlowEngine callFlowEngine,
        PromptService promptService,
        TranscriptRoutingService transcriptRouting,
        ExternalSystemIntegrationService externalIntegration,
        ICosmosDbService cosmosDb,
        ILogger<CallbackHandler> logger)
    {
        _callClient = callClient;
        _callFlowEngine = callFlowEngine;
        _promptService = promptService;
        _transcriptRouting = transcriptRouting;
        _externalIntegration = externalIntegration;
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [Function("CallbackHandler")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "callbacks/{callId}")] HttpRequest req,
        string callId)
    {
        _logger.LogInformation("Callback received for call {CallId}", callId);

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogDebug("Callback body ({Length} bytes): {Body}",
            requestBody.Length, requestBody[..Math.Min(500, requestBody.Length)]);

        CallAutomationEventBase[] parsedEvents;
        try
        {
            var cloudEventsArray = Azure.Messaging.CloudEvent.ParseMany(new BinaryData(requestBody));
            parsedEvents = cloudEventsArray
                .Select(e =>
                {
                    try
                    {
                        return CallAutomationEventParser.Parse(e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse CloudEvent type={Type}", e.Type);
                        return null;
                    }
                })
                .Where(e => e != null)
                .ToArray()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CloudEvents from callback body");
            return new OkResult(); // Don't return 500 — the mock would log it as failure
        }

        foreach (var cloudEvent in parsedEvents)
        {
            _logger.LogInformation("Processing event: {EventType}", cloudEvent.GetType().Name);

            try
            {
                switch (cloudEvent)
                {
                    case CallConnected callConnected:
                        await HandleCallConnectedAsync(callConnected, callId);
                        break;

                    case RecognizeCompleted recognizeCompleted:
                        await HandleRecognizeCompletedAsync(recognizeCompleted, callId);
                        break;

                    case RecognizeFailed recognizeFailed:
                        await HandleRecognizeFailedAsync(recognizeFailed, callId);
                        break;

                    case PlayCompleted playCompleted:
                        _logger.LogInformation("Play completed for call {CallId}", callId);
                        break;

                    case PlayFailed playFailed:
                        _logger.LogWarning("Play failed for call {CallId}: {Reason}",
                            callId, playFailed.ResultInformation?.Message);
                        break;

                    case CallDisconnected callDisconnected:
                        await HandleCallDisconnectedAsync(callDisconnected, callId);
                        break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", cloudEvent.GetType().Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType} for call {CallId}",
                    cloudEvent.GetType().Name, callId);
            }
        }

        return new OkResult();
    }

    /// <summary>
    /// When the call connects, play the welcome prompt and start the first menu.
    /// </summary>
    private async Task HandleCallConnectedAsync(CallConnected callConnected, string callId)
    {
        _logger.LogInformation("Call connected: {CallId}", callId);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        var callerNumber = callLog?.CallerNumber ?? "unknown";

        // Track caller for this connection so PlayMenu can use it as targetParticipant
        _callerByConnection[callConnected.CallConnectionId] = callerNumber;

        // Resolve the appropriate menu based on caller context
        var menu = await _callFlowEngine.ResolveMenuAsync(callerNumber);

        // Track menu navigation
        if (callLog != null)
        {
            callLog.MenuPath.Add(new MenuPathEntry
            {
                MenuId = menu.Id,
                MenuName = menu.Name,
                Timestamp = DateTime.UtcNow
            });
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        // Play the menu prompt and collect DTMF input
        await PlayMenuAndCollectInputAsync(callConnected.CallConnectionId, menu);
    }

    /// <summary>
    /// Handle DTMF/speech recognition result and navigate the IVR tree.
    /// For speech-routing menus, the transcript is sent to Azure OpenAI for intent classification.
    /// </summary>
    private async Task HandleRecognizeCompletedAsync(RecognizeCompleted recognizeCompleted, string callId)
    {
        string input;
        bool isSpeechInput = false;

        if (recognizeCompleted.RecognizeResult is DtmfResult dtmfResult)
        {
            input = string.Join("", dtmfResult.Tones.Select(t => DtmfToneToString(t)));
            _logger.LogInformation("DTMF input received: {Input} for call {CallId}", input, callId);
        }
        else if (recognizeCompleted.RecognizeResult is SpeechResult speechResult)
        {
            input = speechResult.Speech ?? "";
            isSpeechInput = true;
            _logger.LogInformation("Speech input received: {Input} for call {CallId}", input, callId);
        }
        else
        {
            _logger.LogWarning("Unknown recognition result type for call {CallId}", callId);
            return;
        }

        // Get current menu from call log
        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        var currentMenuId = callLog?.MenuPath.LastOrDefault()?.MenuId;

        if (currentMenuId == null)
        {
            _logger.LogError("No current menu found for call {CallId}", callId);
            return;
        }

        var currentMenu = await _cosmosDb.GetMenuAsync(currentMenuId);

        // ─── Speech Routing (AI-based transcript classification) ────
        if (isSpeechInput && currentMenu?.EnableSpeechRecognition == true)
        {
            await HandleSpeechRoutingAsync(recognizeCompleted.CallConnectionId, input, currentMenu, callId, callLog);
            return;
        }

        // ─── Standard DTMF / keyword routing ───────────────────────
        var action = await _callFlowEngine.ProcessInputAsync(currentMenuId, input);

        // Log the input
        if (callLog != null)
        {
            var lastEntry = callLog.MenuPath.LastOrDefault();
            if (lastEntry != null)
                lastEntry.Input = input;
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        // Execute the resulting action
        await ExecuteActionAsync(recognizeCompleted.CallConnectionId, action, callId, callLog);
    }

    /// <summary>
    /// Handle speech-based routing: send transcript to Azure OpenAI for intent classification,
    /// then route to the matching team or trigger fallback.
    /// </summary>
    private async Task HandleSpeechRoutingAsync(string callConnectionId, string transcript, IvrMenu menu, string callId, CallLog? callLog)
    {
        _logger.LogInformation("Processing speech routing for call {CallId}. Transcript: \"{Transcript}\"", callId, transcript);

        // Save transcript to call log immediately
        if (callLog != null)
        {
            callLog.Transcript = transcript;
            var lastEntry = callLog.MenuPath.LastOrDefault();
            if (lastEntry != null)
                lastEntry.Input = $"[speech] {transcript}";
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        // Classify the transcript against configured teams
        var teamConfigIds = menu.TeamRoutingConfigIds;
        var routingResult = await _transcriptRouting.ClassifyAndRouteAsync(transcript, teamConfigIds);

        // Update call log with classification results
        if (callLog != null)
        {
            callLog.DetectedIntent = routingResult.DetectedIntent;
            callLog.IntentConfidence = routingResult.Confidence;
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        if (routingResult.IsMatched && routingResult.MatchedTeam != null)
        {
            var team = routingResult.MatchedTeam;

            _logger.LogInformation(
                "Call {CallId} routed to team \"{TeamName}\" (confidence: {Confidence:P0})",
                callId, team.TeamName, routingResult.Confidence);

            // Update call log with routing info
            if (callLog != null)
            {
                callLog.RoutedToTeam = team.TeamName;
                callLog.Status = CallStatus.Transferred;
                callLog.TransferredTo = team.TransferNumber;
                callLog.Disposition = !string.IsNullOrEmpty(team.QueueName)
                    ? CallDisposition.TransferredToQueue
                    : CallDisposition.TransferredToExternal;
                callLog.QueueName = team.QueueName;
                await _cosmosDb.UpdateCallLogAsync(callLog);
            }

            var callConnection = _callClient.GetCallConnection(callConnectionId);

            // Play confirmation prompt if configured, then transfer
            if (team.ConfirmationPromptId != null)
            {
                var confirmPrompt = await _promptService.ResolvePromptAsync(team.ConfirmationPromptId);
                var confirmSource = BuildPlaySource(confirmPrompt);
                await callConnection.GetCallMedia().PlayToAllAsync(confirmSource);
                await Task.Delay(2000); // Brief pause for the confirmation to play
            }
            else
            {
                // Play a default transfer message using TTS
                var transferMsg = new TextSource($"I'll connect you with our {team.TeamName} team now. Please hold.")
                {
                    VoiceName = "en-US-JennyNeural"
                };
                await callConnection.GetCallMedia().PlayToAllAsync(transferMsg);
                await Task.Delay(2000);
            }

            // Transfer the call
            await callConnection.TransferCallToParticipantAsync(
                new PhoneNumberIdentifier(team.TransferNumber));
        }
        else
        {
            _logger.LogInformation(
                "No confident team match for call {CallId}. Intent: \"{Intent}\", Confidence: {Confidence:P0}",
                callId, routingResult.DetectedIntent, routingResult.Confidence);

            // Use the menu's speech fallback action, or replay the menu
            if (menu.SpeechFallbackAction != null)
            {
                await ExecuteActionAsync(callConnectionId, menu.SpeechFallbackAction, callId, callLog);
            }
            else
            {
                // Default fallback: apologize and replay
                var callConnection = _callClient.GetCallConnection(callConnectionId);
                var fallbackMsg = new TextSource("I'm sorry, I didn't quite get that. Let me try again.")
                {
                    VoiceName = "en-US-JennyNeural"
                };
                await callConnection.GetCallMedia().PlayToAllAsync(fallbackMsg);
                await Task.Delay(2000);
                await PlayMenuAndCollectInputAsync(callConnectionId, menu);
            }
        }
    }

    /// <summary>
    /// Handle recognition failure — replay the menu or offer a timeout action.
    /// </summary>
    private async Task HandleRecognizeFailedAsync(RecognizeFailed recognizeFailed, string callId)
    {
        _logger.LogWarning("Recognition failed for call {CallId}: {Reason}",
            callId, recognizeFailed.ResultInformation?.Message);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);
        var currentMenuId = callLog?.MenuPath.LastOrDefault()?.MenuId;

        if (currentMenuId != null)
        {
            var menu = await _cosmosDb.GetMenuAsync(currentMenuId);
            if (menu != null)
            {
                // Replay the timeout prompt and menu
                if (menu.TimeoutPromptId != null)
                {
                    var timeoutPrompt = await _promptService.ResolvePromptAsync(menu.TimeoutPromptId);
                    // Could play timeout prompt here, then re-collect
                }

                await PlayMenuAndCollectInputAsync(recognizeFailed.CallConnectionId, menu);
            }
        }
    }

    /// <summary>
    /// Handle call disconnection — optionally transfer to an Avaya CM10 VDN,
    /// then finalize the call log.
    /// </summary>
    private async Task HandleCallDisconnectedAsync(CallDisconnected callDisconnected, string callId)
    {
        _logger.LogInformation("Call disconnected: {CallId}", callId);

        var callLog = await _cosmosDb.GetCallLogByCallIdAsync(callId);

        // ─── CM10 VDN Transfer on Disconnect ────────────────────
        // If configured, transfer the call to the Avaya VDN so that
        // the CM10 ACD can handle post-IVR routing.
        var systemConfig = await _cosmosDb.GetSystemConfigAsync();

        if (systemConfig.EnableDisconnectTransferToVdn
            && !string.IsNullOrEmpty(systemConfig.DefaultCm10Vdn)
            && callLog?.Status != CallStatus.Transferred) // avoid double-transfer
        {
            _logger.LogInformation(
                "Transferring disconnect to CM10 VDN {Vdn} for call {CallId}",
                systemConfig.DefaultCm10Vdn, callId);

            try
            {
                // Resolve the SBC FQDN for the VDN transfer
                var sbcFqdn = systemConfig.Cm10SbcFqdn ?? systemConfig.DefaultSbcFqdn;

                await TransferToVdnAsync(
                    callDisconnected.CallConnectionId,
                    systemConfig.DefaultCm10Vdn,
                    sbcFqdn,
                    systemConfig.Cm10TransferPromptId);

                if (callLog != null)
                {
                    callLog.Status = CallStatus.Transferred;
                    callLog.TransferredTo = systemConfig.DefaultCm10Vdn;
                    callLog.Disposition = CallDisposition.TransferredToVdn;
                    callLog.EndTime = DateTime.UtcNow;
                    callLog.DurationSeconds = (callLog.EndTime.Value - callLog.StartTime).TotalSeconds;
                    await _cosmosDb.UpdateCallLogAsync(callLog);
                }

                return; // VDN transfer initiated — do not finalize as hangup
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to transfer to CM10 VDN {Vdn} for call {CallId}. Finalizing as normal disconnect.",
                    systemConfig.DefaultCm10Vdn, callId);
            }
        }

        // ─── Normal disconnect finalization ──────────────────────
        if (callLog != null)
        {
            callLog.Status = CallStatus.Completed;
            callLog.EndTime = DateTime.UtcNow;
            callLog.DurationSeconds = (callLog.EndTime.Value - callLog.StartTime).TotalSeconds;
            callLog.Disposition = CallDisposition.CallerHangup;
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }
    }

    /// <summary>
    /// Play a menu's prompt and collect input — either DTMF or speech depending on menu configuration.
    /// </summary>
    private async Task PlayMenuAndCollectInputAsync(string callConnectionId, IvrMenu menu)
    {
        var callConnection = _callClient.GetCallConnection(callConnectionId);

        // Resolve caller identifier for targetParticipant (required by ACS SDK)
        var callerRawId = _callerByConnection.GetValueOrDefault(callConnectionId, "+10000000000");
        var targetParticipant = callerRawId.StartsWith("+")
            ? new PhoneNumberIdentifier(callerRawId) as CommunicationIdentifier
            : CommunicationIdentifier.FromRawId($"4:{callerRawId}");

        // Determine which prompt to use
        var promptId = menu.EnableSpeechRecognition && menu.SpeechRoutingPromptId != null
            ? menu.SpeechRoutingPromptId
            : menu.PromptId;

        var promptContent = await _promptService.ResolvePromptAsync(promptId);
        var playSource = BuildPlaySource(promptContent);

        if (menu.EnableSpeechRecognition)
        {
            // ─── Speech Recognition Mode ────────────────────────────
            _logger.LogInformation("Starting speech recognition for menu {MenuName}", menu.Name);

            var recognizeOptions = new CallMediaRecognizeSpeechOptions(
                targetParticipant: targetParticipant)
            {
                Prompt = playSource,
                EndSilenceTimeout = TimeSpan.FromMilliseconds(1500),
                SpeechLanguage = "en-US"
            };

            await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }
        else
        {
            // ─── DTMF Mode (existing behavior) ─────────────────────
            var maxDigits = menu.Options.Count > 0
                ? menu.Options.Max(o => o.DtmfKey.Length)
                : 1;

            var recognizeOptions = new CallMediaRecognizeDtmfOptions(
                targetParticipant: targetParticipant,
                maxTonesToCollect: maxDigits)
            {
                Prompt = playSource,
                InitialSilenceTimeout = TimeSpan.FromSeconds(menu.TimeoutSeconds),
                InterToneTimeout = TimeSpan.FromSeconds(5),
                InterruptPrompt = true
            };

            await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }
    }

    /// <summary>
    /// Execute a menu action (navigate, transfer, play, hang up, etc.)
    /// </summary>
    private async Task ExecuteActionAsync(string callConnectionId, MenuAction action, string callId, CallLog? callLog)
    {
        var callConnection = _callClient.GetCallConnection(callConnectionId);

        switch (action.Type)
        {
            case ActionType.NavigateToMenu:
                if (action.TargetMenuId != null)
                {
                    var targetMenu = await _cosmosDb.GetMenuAsync(action.TargetMenuId);
                    if (targetMenu != null)
                    {
                        // Track navigation
                        if (callLog != null)
                        {
                            callLog.MenuPath.Add(new MenuPathEntry
                            {
                                MenuId = targetMenu.Id,
                                MenuName = targetMenu.Name,
                                Timestamp = DateTime.UtcNow
                            });
                            await _cosmosDb.UpdateCallLogAsync(callLog);
                        }

                        await PlayMenuAndCollectInputAsync(callConnectionId, targetMenu);
                    }
                }
                break;

            case ActionType.TransferToNumber:
                if (action.TransferNumber != null)
                {
                    _logger.LogInformation("Transferring call {CallId} to {Number}", callId, action.TransferNumber);

                    if (callLog != null)
                    {
                        callLog.Status = CallStatus.Transferred;
                        callLog.TransferredTo = action.TransferNumber;
                        callLog.Disposition = CallDisposition.TransferredToExternal;
                        await _cosmosDb.UpdateCallLogAsync(callLog);
                    }

                    await callConnection.TransferCallToParticipantAsync(
                        new PhoneNumberIdentifier(action.TransferNumber));
                }
                break;

            case ActionType.TransferToQueue:
            case ActionType.TransferToTeam:
                if (!string.IsNullOrEmpty(action.QueueName))
                {
                    _logger.LogInformation("Transferring call {CallId} to queue {Queue}", callId, action.QueueName);

                    // Play hold prompt if configured
                    if (!string.IsNullOrEmpty(action.PromptId))
                    {
                        var holdPrompt = await _promptService.ResolvePromptAsync(action.PromptId);
                        var holdSource = BuildPlaySource(holdPrompt);
                        await callConnection.GetCallMedia().PlayToAllAsync(holdSource);
                        await Task.Delay(1500);
                    }

                    if (callLog != null)
                    {
                        callLog.Status = CallStatus.Transferred;
                        callLog.QueueName = action.QueueName;
                        callLog.Disposition = CallDisposition.TransferredToQueue;
                        await _cosmosDb.UpdateCallLogAsync(callLog);
                    }

                    // Transfer via SIP to a CM10 queue VDN.
                    // The mock ACS bridge + CM10 simulator handle queue routing.
                    var systemConfig = await _cosmosDb.GetSystemConfigAsync();
                    var sbcFqdn = systemConfig.Cm10SbcFqdn ?? systemConfig.DefaultSbcFqdn ?? "sbc.yourdomain.com";
                    var queueSipTarget = $"sip:queue-{action.QueueName}@{sbcFqdn}";
                    var queueTarget = new CommunicationUserIdentifier(queueSipTarget);
                    await callConnection.TransferCallToParticipantAsync(queueTarget);
                }
                else
                {
                    _logger.LogWarning("TransferToQueue action on call {CallId} has no queue name", callId);
                }
                break;

            case ActionType.PlayPrompt:
                if (action.PromptId != null)
                {
                    var prompt = await _promptService.ResolvePromptAsync(action.PromptId);
                    var playSource = BuildPlaySource(prompt);
                    await callConnection.GetCallMedia().PlayToAllAsync(playSource);
                }
                break;

            case ActionType.Hangup:
                _logger.LogInformation("Hanging up call {CallId}", callId);

                if (action.PromptId != null)
                {
                    var goodbyePrompt = await _promptService.ResolvePromptAsync(action.PromptId);
                    var goodbyeSource = BuildPlaySource(goodbyePrompt);
                    await callConnection.GetCallMedia().PlayToAllAsync(goodbyeSource);
                    await Task.Delay(3000); // Brief pause after goodbye
                }

                await callConnection.HangUpAsync(true);
                break;

            case ActionType.RepeatMenu:
                if (action.TargetMenuId != null)
                {
                    var repeatMenu = await _cosmosDb.GetMenuAsync(action.TargetMenuId);
                    if (repeatMenu != null)
                        await PlayMenuAndCollectInputAsync(callConnectionId, repeatMenu);
                }
                break;

            case ActionType.Webhook:
                await HandleWebhookActionAsync(callConnectionId, action, callId, callLog);
                break;

            case ActionType.SubmitToExternalSystem:
                await HandleSubmitToExternalSystemAsync(callConnectionId, action, callId, callLog);
                break;

            case ActionType.TransferToVdn:
                await HandleTransferToVdnActionAsync(callConnectionId, action, callId, callLog);
                break;

            default:
                _logger.LogWarning("Unhandled action type: {ActionType}", action.Type);
                break;
        }
    }

    /// <summary>
    /// Handle a webhook action — POST collected IVR data to the configured external
    /// API URL, play a confirmation prompt, update the call log, and hang up.
    /// </summary>
    private async Task HandleWebhookActionAsync(string callConnectionId, MenuAction action, string callId, CallLog? callLog)
    {
        if (string.IsNullOrEmpty(action.WebhookUrl))
        {
            _logger.LogWarning("Webhook action on call {CallId} has no WebhookUrl configured", callId);
            return;
        }

        var callConnection = _callClient.GetCallConnection(callConnectionId);

        _logger.LogInformation("Submitting collected data for call {CallId} to {Url}", callId, action.WebhookUrl);

        try
        {
            // ── Build enriched collected-data from the menu path ──
            var collectedData = new Dictionary<string, string>();
            if (callLog?.MenuPath != null)
            {
                foreach (var entry in callLog.MenuPath)
                {
                    // Look up the menu to resolve the selected option's label
                    var menu = await _cosmosDb.GetMenuAsync(entry.MenuId);
                    var selectedLabel = menu?.Options
                        .FirstOrDefault(o => o.DtmfKey == entry.Input)?.Label ?? entry.Input;
                    collectedData[entry.MenuId] = selectedLabel ?? "";
                }
            }

            var payload = new
            {
                callId,
                callerNumber = callLog?.CallerNumber,
                calledNumber = callLog?.CalledNumber,
                callerName = callLog?.AniData?.CallerName,
                collectedData,
                menuPath = callLog?.MenuPath,
                metadata = callLog?.Metadata,
                customData = action.CustomData,
                timestamp = DateTime.UtcNow
            };

            var httpClient = new HttpClient();
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Webhook payload for call {CallId}:\n{Payload}", callId, jsonPayload);

            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(action.WebhookUrl, content);

            _logger.LogInformation("External API responded {StatusCode} for call {CallId}",
                response.StatusCode, callId);

            // Update call log with submission result
            if (callLog != null)
            {
                callLog.ExtractedData = collectedData;
                callLog.Status = CallStatus.Completed;
                callLog.Disposition = CallDisposition.Completed;
                callLog.Metadata["externalApiUrl"] = action.WebhookUrl;
                callLog.Metadata["externalApiStatus"] = response.StatusCode.ToString();
                await _cosmosDb.UpdateCallLogAsync(callLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook call failed for {CallId} to {Url}", callId, action.WebhookUrl);
        }

        // ── Play confirmation prompt and hang up ──
        if (!string.IsNullOrEmpty(action.PromptId))
        {
            try
            {
                var confirmPrompt = await _promptService.ResolvePromptAsync(action.PromptId);
                var confirmSource = BuildPlaySource(confirmPrompt);
                await callConnection.GetCallMedia().PlayToAllAsync(confirmSource);
                await Task.Delay(3000); // Allow prompt to play before disconnect
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play confirmation prompt for call {CallId}", callId);
            }
        }

        // Hang up after submission
        try
        {
            await callConnection.HangUpAsync(true);
            _logger.LogInformation("Call {CallId} completed after external API submission", callId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hang up call {CallId} after webhook", callId);
        }
    }

    /// <summary>
    /// Handle SubmitToExternalSystem: extract structured data from the caller's transcript
    /// using AI, submit to the configured external system, and confirm back to the caller.
    /// </summary>
    private async Task HandleSubmitToExternalSystemAsync(string callConnectionId, MenuAction action, string callId, CallLog? callLog)
    {
        if (string.IsNullOrEmpty(action.DataExtractionConfigId))
        {
            _logger.LogError("SubmitToExternalSystem action on call {CallId} has no DataExtractionConfigId", callId);
            return;
        }

        var callConnection = _callClient.GetCallConnection(callConnectionId);
        var transcript = callLog?.Transcript ?? "";
        var callerNumber = callLog?.CallerNumber ?? "unknown";

        if (string.IsNullOrWhiteSpace(transcript))
        {
            _logger.LogWarning("No transcript available for external system submission on call {CallId}", callId);

            var noTranscriptMsg = new TextSource("I'm sorry, I wasn't able to capture your request. Let me transfer you to an agent.")
            {
                VoiceName = "en-US-JennyNeural"
            };
            await callConnection.GetCallMedia().PlayToAllAsync(noTranscriptMsg);
            return;
        }

        _logger.LogInformation("Processing external system submission for call {CallId}, config {ConfigId}",
            callId, action.DataExtractionConfigId);

        // Play a brief hold message while processing
        var holdMsg = new TextSource("Thank you. Let me process that for you.")
        {
            VoiceName = "en-US-JennyNeural"
        };
        await callConnection.GetCallMedia().PlayToAllAsync(holdMsg);

        // Additional context from the action's custom data
        var additionalContext = action.CustomData.Count > 0 ? action.CustomData : null;

        // Extract and submit
        var result = await _externalIntegration.ExtractAndSubmitAsync(
            transcript, action.DataExtractionConfigId, callerNumber, additionalContext);

        // Update call log
        if (callLog != null)
        {
            callLog.ExternalSystemResults.Add(result);

            // Store extracted data if available from the extraction config
            var extractionConfig = await _cosmosDb.GetDataExtractionConfigAsync(action.DataExtractionConfigId);

            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        // Play result message to the caller
        if (result.Success)
        {
            _logger.LogInformation("External system submission succeeded for call {CallId}. Confirmation: {Value}",
                callId, result.ConfirmationValue);

            // Try to load a success prompt or build a TTS confirmation
            var extractionConfig = await _cosmosDb.GetDataExtractionConfigAsync(action.DataExtractionConfigId);
            string successMessage;

            if (extractionConfig?.SuccessTtsTemplate != null && callLog?.ExtractedData != null)
            {
                successMessage = ExternalSystemIntegrationService.BuildConfirmationMessage(
                    extractionConfig.SuccessTtsTemplate,
                    callLog.ExtractedData,
                    result.ConfirmationValue);
            }
            else if (result.ConfirmationValue != null)
            {
                successMessage = $"Done. Your reference number is {result.ConfirmationValue}. " +
                                 "Is there anything else I can help you with?";
            }
            else
            {
                successMessage = "Your request has been submitted successfully. Is there anything else I can help you with?";
            }

            var successSource = new TextSource(successMessage)
            {
                VoiceName = "en-US-JennyNeural"
            };
            await callConnection.GetCallMedia().PlayToAllAsync(successSource);

            // Execute post-submit action if configured
            if (extractionConfig?.PostSubmitAction != null)
            {
                await Task.Delay(3000); // Let the success message play
                await ExecuteActionAsync(callConnectionId, extractionConfig.PostSubmitAction, callId, callLog);
            }
        }
        else
        {
            _logger.LogWarning("External system submission failed for call {CallId}: {Error}",
                callId, result.ErrorMessage);

            var extractionConfig = await _cosmosDb.GetDataExtractionConfigAsync(action.DataExtractionConfigId);

            if (extractionConfig?.FailurePromptId != null)
            {
                var failPrompt = await _promptService.ResolvePromptAsync(extractionConfig.FailurePromptId);
                var failSource = BuildPlaySource(failPrompt);
                await callConnection.GetCallMedia().PlayToAllAsync(failSource);
            }
            else
            {
                var failMsg = new TextSource(
                    "I'm sorry, I wasn't able to process that request right now. " +
                    "Let me transfer you to someone who can help.")
                {
                    VoiceName = "en-US-JennyNeural"
                };
                await callConnection.GetCallMedia().PlayToAllAsync(failMsg);
            }

            // Execute post-submit action (e.g., transfer to agent as fallback)
            if (extractionConfig?.PostSubmitAction != null)
            {
                await Task.Delay(3000);
                await ExecuteActionAsync(callConnectionId, extractionConfig.PostSubmitAction, callId, callLog);
            }
        }
    }

    private static PlaySource BuildPlaySource(PromptContent prompt)
    {
        return prompt.Type switch
        {
            PromptType.Tts => new TextSource(prompt.Text ?? "Please wait.")
            {
                VoiceName = prompt.Voice ?? "en-US-JennyNeural"
            },
            PromptType.AudioFile => new FileSource(new Uri(prompt.AudioUrl!)),
            PromptType.Ssml => new SsmlSource(prompt.SsmlContent!),
            _ => new TextSource("Please wait.")
        };
    }

    private static string DtmfToneToString(DtmfTone tone)
    {
        return tone.ToString() switch
        {
            "Zero" => "0", "One" => "1", "Two" => "2", "Three" => "3",
            "Four" => "4", "Five" => "5", "Six" => "6", "Seven" => "7",
            "Eight" => "8", "Nine" => "9", "Pound" => "#", "Asterisk" => "*",
            _ => tone.ToString()
        };
    }

    private static DtmfTone? StringToDtmfTone(string key)
    {
        return key switch
        {
            "0" => DtmfTone.Zero, "1" => DtmfTone.One, "2" => DtmfTone.Two,
            "3" => DtmfTone.Three, "4" => DtmfTone.Four, "5" => DtmfTone.Five,
            "6" => DtmfTone.Six, "7" => DtmfTone.Seven, "8" => DtmfTone.Eight,
            "9" => DtmfTone.Nine, "#" => DtmfTone.Pound, "*" => DtmfTone.Asterisk,
            _ => null
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Avaya CM10 VDN Transfer Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Handle TransferToVdn menu action — transfer the call to an Avaya CM10 VDN.
    /// </summary>
    private async Task HandleTransferToVdnActionAsync(
        string callConnectionId, MenuAction action, string callId, CallLog? callLog)
    {
        // Resolve the VDN address: action-level override → system default
        var systemConfig = await _cosmosDb.GetSystemConfigAsync();
        var vdnAddress = action.VdnAddress ?? systemConfig.DefaultCm10Vdn;

        if (string.IsNullOrEmpty(vdnAddress))
        {
            _logger.LogError("TransferToVdn action on call {CallId} has no VDN address configured", callId);
            return;
        }

        var sbcFqdn = systemConfig.Cm10SbcFqdn ?? systemConfig.DefaultSbcFqdn;

        _logger.LogInformation("Transferring call {CallId} to CM10 VDN {Vdn}", callId, vdnAddress);

        if (callLog != null)
        {
            callLog.Status = CallStatus.Transferred;
            callLog.TransferredTo = vdnAddress;
            callLog.Disposition = CallDisposition.TransferredToVdn;
            await _cosmosDb.UpdateCallLogAsync(callLog);
        }

        await TransferToVdnAsync(callConnectionId, vdnAddress, sbcFqdn, action.PromptId);
    }

    /// <summary>
    /// Transfer a live call to an Avaya CM10 VDN.
    /// Supports both SIP URI and E.164 targets.
    /// </summary>
    private async Task TransferToVdnAsync(
        string callConnectionId, string vdnAddress, string? sbcFqdn, string? preTransferPromptId)
    {
        var callConnection = _callClient.GetCallConnection(callConnectionId);

        // Play a pre-transfer prompt if configured
        if (!string.IsNullOrEmpty(preTransferPromptId))
        {
            var prompt = await _promptService.ResolvePromptAsync(preTransferPromptId);
            var source = BuildPlaySource(prompt);
            await callConnection.GetCallMedia().PlayToAllAsync(source);
            await Task.Delay(2000);
        }

        // Determine the target identifier
        CommunicationIdentifier targetIdentifier;

        if (vdnAddress.StartsWith("sip:", StringComparison.OrdinalIgnoreCase))
        {
            // SIP URI — direct SIP transfer to the VDN on the Avaya SBC
            // e.g. "sip:70100@sbc.contoso.com"
            targetIdentifier = new CommunicationUserIdentifier(vdnAddress);
        }
        else if (!string.IsNullOrEmpty(sbcFqdn) && !vdnAddress.Contains("@"))
        {
            // Extension / VDN number — build a SIP URI using the configured SBC
            // e.g. VDN "70100" + SBC "sbc.contoso.com" → "sip:70100@sbc.contoso.com"
            var sipUri = $"sip:{vdnAddress}@{sbcFqdn}";
            targetIdentifier = new CommunicationUserIdentifier(sipUri);
        }
        else
        {
            // E.164 phone number — route via PSTN
            targetIdentifier = new PhoneNumberIdentifier(vdnAddress);
        }

        _logger.LogInformation("Executing VDN transfer to {Target}", vdnAddress);
        await callConnection.TransferCallToParticipantAsync(targetIdentifier);
    }
}
