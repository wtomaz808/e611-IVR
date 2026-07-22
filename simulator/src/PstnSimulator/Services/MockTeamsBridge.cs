using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Teams calling bot simulation — mirrors what Microsoft Teams does when a PSTN call
/// arrives for a Teams Resource Account.
///
/// Instead of sending ACS EventGrid payloads, this bridge sends Microsoft Graph
/// commsNotifications JSON to the IVR's /api/bot-messages endpoint.
///
/// The simulator also exposes a mock Microsoft Graph Calling API at /graph/v1.0
/// (configured via GraphApiEndpoint in IVR.Functions local.settings.json) so the
/// IVR's GraphServiceClient connects to the simulator rather than real Azure.
///
/// Flow:
///   1. OriginateCallAsync  → POST /api/bot-messages  (state: "incoming")
///   2. IVR calls /graph/.../answer  → simulate receives it → POST "established"
///   3. IVR calls /graph/.../playPrompt → simulate → POST "playPromptOperation completed"
///   4. SendDtmfAsync       → POST /api/bot-messages  (toneInfo in call notification)
///   5. SendSpeechAsync     → sets transcript, POST "recordOperation completed"
///   6. DisconnectAsync     → POST /api/bot-messages  (state: "terminated")
/// </summary>
public class MockTeamsBridge : IAcsBridge
{
    private readonly CallStateManager _callManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MockTeamsBridge> _logger;

    // Tracks which callId is waiting for a specific Graph API call
    // Key = callId, Value = next expected operation
    private readonly Dictionary<string, string> _pendingOps = new();

    public MockTeamsBridge(
        CallStateManager callManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<MockTeamsBridge> logger)
    {
        _callManager       = callManager;
        _httpClientFactory = httpClientFactory;
        _config            = config;
        _logger            = logger;
    }

    // ════════════════════════════════════════════════════════════
    //  IAcsBridge implementation
    // ════════════════════════════════════════════════════════════

    /// <summary>Originate: send a Graph commsNotification with state="incoming".</summary>
    public async Task<bool> OriginateCallAsync(SimulatedCall call)
    {
        var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";

        _callManager.GetCall(call.Id); // ensure call is accessible via manager
        _pendingOps[call.Id] = "waiting_answer";

        var payload = BuildCallStateNotification(call.Id, "incoming",
            callerNumber: call.CallerNumber,
            calledNumber: call.DidNumber);

        _callManager.TransitionCall(call.Id, CallState.SentToIvr, CallEventSource.Acs,
            "commsNotification (incoming) sent to Teams bot",
            $"POST {ivrEndpoint}/api/bot-messages");

        return await PostToBotEndpointAsync(ivrEndpoint, payload, call,
            "Teams commsNotification (incoming)");
    }

    /// <summary>DTMF: send a toneInfo notification inside a call state update.</summary>
    public async Task SendDtmfAsync(string callId, string tone)
    {
        var call = _callManager.GetCall(callId);
        if (call == null) return;

        var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";

        // Map simulator digit to Graph tone enum string
        var graphTone = MapDigitToGraphTone(tone);
        _callManager.UpdateCall(callId, c =>
        {
            c.DtmfInputs.Add(tone);
            c.AddEvent(CallEventSource.Pstn, $"DTMF: {tone} → Graph tone: {graphTone}");
        });

        var payload = BuildToneNotification(callId, graphTone);
        await PostToBotEndpointAsync(ivrEndpoint, payload, call,
            $"Teams toneInfo ({graphTone})");
    }

    /// <summary>
    /// Speech: store transcript in call state, then send a recordOperation
    /// completed notification so the bot processes the speech.
    /// </summary>
    public async Task SendSpeechAsync(string callId, string text)
    {
        var call = _callManager.GetCall(callId);
        if (call == null) return;

        var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";

        _callManager.UpdateCall(callId, c =>
        {
            c.SpeechInputs.Add(text);
            c.AddEvent(CallEventSource.Pstn, $"Speech: \"{text}\"");
        });

        var operationId = Guid.NewGuid().ToString();
        var payload = BuildRecordOperationNotification(callId, operationId, text);
        await PostToBotEndpointAsync(ivrEndpoint, payload, call,
            $"Teams recordOperation completed (transcript: \"{text}\")");
    }

    /// <summary>Disconnect: send a terminated state notification and transition to Disconnected.</summary>
    public async Task DisconnectAsync(string callId)
    {
        var call = _callManager.GetCall(callId);
        if (call == null) return;

        var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";
        _callManager.UpdateCall(callId, c => c.AddEvent(CallEventSource.Pstn, "Caller disconnected"));

        var payload = BuildCallStateNotification(callId, "terminated");
        await PostToBotEndpointAsync(ivrEndpoint, payload, call,
            "Teams commsNotification (terminated)");

        // Transition to Disconnected so the simulator UI clears the active call.
        _callManager.TransitionCall(callId, CallState.Disconnected, CallEventSource.Pstn, "Caller disconnected");
    }

    // ════════════════════════════════════════════════════════════
    //  Mock Graph Calling API request handler
    //  Called from simulator Program.cs for /graph/v1.0/* routes
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Handle an IVR Graph API call.  The IVR's GraphServiceClient is configured to
    /// point at the simulator (GraphApiEndpoint = http://pstn-simulator:8080/graph/v1.0).
    /// After each command, fire the appropriate follow-up notification back to the IVR.
    /// </summary>
    public async Task<IResult> HandleGraphApiAsync(HttpContext ctx, string path)
    {
        _logger.LogInformation("Mock Graph API: {Method} /graph/v1.0/{Path}", ctx.Request.Method, path);

        // Extract call ID from path: communications/calls/{callId}[/action]
        var callId = ExtractCallId(path);
        if (callId == null)
            return Results.Ok(); // Non-call path (e.g. token endpoints)

        var action = path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : null;
        var call   = callId != null ? _callManager.GetCall(callId) : null;

        // DELETE = hang up
        if (ctx.Request.Method == "DELETE")
        {
            _callManager.TransitionCall(callId, CallState.Disconnected, CallEventSource.Ivr, "Graph DELETE (hang up)");
            return Results.NoContent();
        }

        return action switch
        {
            "answer" => await HandleAnswerAsync(callId!, call),
            "playPrompt" => await HandlePlayPromptAsync(ctx, callId!, call),
            "subscribeToTone" => HandleSubscribeToTone(callId!, call),
            "recordResponse" => HandleRecordResponse(callId!, call),
            "transfer" => await HandleTransferAsync(ctx, callId!, call),
            "reject" => HandleReject(callId!, call),
            _ => Results.Ok()
        };
    }

    // ── Graph API action handlers ────────────────────────────────────────────

    private async Task<IResult> HandleAnswerAsync(string callId, SimulatedCall? call)
    {
        _callManager.TransitionCall(callId, CallState.IvrAnswered, CallEventSource.Ivr, "Graph answer");

        // Fire "established" notification after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";
            var payload = BuildCallStateNotification(callId, "established");
            await PostToBotEndpointRawAsync(ivrEndpoint, payload);
        });

        return Results.Ok(new { id = callId, state = "establishing" });
    }

    private async Task<IResult> HandlePlayPromptAsync(HttpContext ctx, string callId, SimulatedCall? call)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation("PlayPrompt for call {CallId}: {Body}", callId, body[..Math.Min(200, body.Length)]);

        // Extract audio URL from the playPrompt body so the simulator UI can play it
        string? audioUrl = null;
        try
        {
            var doc = JsonDocument.Parse(body);
            audioUrl = doc.RootElement
                .GetProperty("prompts")[0]
                .GetProperty("mediaInfo")
                .GetProperty("uri")
                .GetString();
        }
        catch { /* ignore parse errors */ }

        _callManager.UpdateCall(callId, c =>
        {
            c.RecognizeType = null;
            if (audioUrl != null) c.CurrentPromptText = audioUrl;
        });
        _callManager.TransitionCall(callId, CallState.IvrPlaying, CallEventSource.Ivr, "Graph playPrompt — IVR playing audio");

        var operationId = Guid.NewGuid().ToString();

        // Fire "playPromptOperation completed" after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(500); // Simulate prompt playing
            var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";
            var payload = BuildPlayPromptOperationNotification(callId, operationId);
            await PostToBotEndpointRawAsync(ivrEndpoint, payload);
        });

        return Results.Accepted($"/graph/v1.0/communications/calls/{callId}/operations/{operationId}",
            new { id = operationId, status = "running" });
    }

    private IResult HandleSubscribeToTone(string callId, SimulatedCall? call)
    {
        _pendingOps[callId] = "subscribed_to_tone";
        _callManager.UpdateCall(callId, c => c.RecognizeType = "dtmf");
        _callManager.TransitionCall(callId, CallState.IvrRecognizing, CallEventSource.Ivr,
            "Graph subscribeToTone — waiting for DTMF");
        return Results.Accepted(value: new { id = Guid.NewGuid().ToString(), status = "running" });
    }

    private IResult HandleRecordResponse(string callId, SimulatedCall? call)
    {
        _pendingOps[callId] = "recording";
        _callManager.UpdateCall(callId, c => c.RecognizeType = "speech");
        _callManager.TransitionCall(callId, CallState.IvrRecognizing, CallEventSource.Ivr,
            "Graph recordResponse — waiting for speech");
        return Results.Accepted(value: new { id = Guid.NewGuid().ToString(), status = "running" });
    }

    private async Task<IResult> HandleTransferAsync(HttpContext ctx, string callId, SimulatedCall? call)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        _logger.LogInformation("Transfer call {CallId}: {Body}", callId, body[..Math.Min(200, body.Length)]);

        _callManager.TransitionCall(callId, CallState.IvrTransfer, CallEventSource.Ivr, "Graph transfer", body[..Math.Min(100, body.Length)]);

        // Fire "terminated" after a short delay to simulate transfer completing
        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";
            var payload = BuildCallStateNotification(callId, "terminated");
            await PostToBotEndpointRawAsync(ivrEndpoint, payload);
        });

        return Results.Accepted(value: new { id = Guid.NewGuid().ToString(), status = "running" });
    }

    private IResult HandleReject(string callId, SimulatedCall? call)
    {
        _callManager.TransitionCall(callId, CallState.Failed, CallEventSource.Ivr, "Graph reject");
        return Results.Ok();
    }

    // ════════════════════════════════════════════════════════════
    //  Notification payload builders
    // ════════════════════════════════════════════════════════════

    private static string BuildCallStateNotification(string callId, string state,
        string? callerNumber = null, string? calledNumber = null)
    {
        var resourceData = new JsonObject
        {
            ["@odata.type"] = "#microsoft.graph.call",
            ["id"]          = callId,
            ["state"]       = state
        };

        if (callerNumber != null)
        {
            resourceData["direction"] = "incoming";
            resourceData["requestedModalities"] = new JsonArray("audio");
            resourceData["mediaConfig"] = new JsonObject
            {
                ["@odata.type"] = "#microsoft.graph.serviceHostedMediaConfig"
            };
            resourceData["source"] = new JsonObject
            {
                ["@odata.type"] = "#microsoft.graph.participantInfo",
                ["identity"]    = new JsonObject
                {
                    ["@odata.type"] = "#microsoft.graph.identitySet",
                    ["phone"]       = new JsonObject
                    {
                        ["@odata.type"] = "#microsoft.graph.identity",
                        ["id"]          = callerNumber
                    }
                }
            };
            resourceData["targets"] = new JsonArray(
                new JsonObject
                {
                    ["@odata.type"] = "#microsoft.graph.invitationParticipantInfo",
                    ["identity"]    = new JsonObject
                    {
                        ["@odata.type"] = "#microsoft.graph.identitySet",
                        ["phone"]       = new JsonObject
                        {
                            ["@odata.type"] = "#microsoft.graph.identity",
                            ["id"]          = calledNumber
                        }
                    }
                }
            );
        }

        var envelope = new JsonObject
        {
            ["@odata.type"] = "#microsoft.graph.commsNotifications",
            ["value"] = new JsonArray(
                new JsonObject
                {
                    ["@odata.type"]  = "#microsoft.graph.commsNotification",
                    ["changeType"]   = state == "incoming" ? "created" : "updated",
                    ["resourceUrl"]  = $"/communications/calls/{callId}",
                    ["resourceData"] = resourceData
                }
            )
        };
        return envelope.ToJsonString();
    }

    private static string BuildToneNotification(string callId, string graphTone)
    {
        var envelope = new JsonObject
        {
            ["@odata.type"] = "#microsoft.graph.commsNotifications",
            ["value"] = new JsonArray(
                new JsonObject
                {
                    ["@odata.type"]  = "#microsoft.graph.commsNotification",
                    ["changeType"]   = "updated",
                    ["resourceUrl"]  = $"/communications/calls/{callId}",
                    ["resourceData"] = new JsonObject
                    {
                        ["@odata.type"] = "#microsoft.graph.call",
                        ["id"]          = callId,
                        ["state"]       = "established",
                        ["toneInfo"]    = new JsonObject
                        {
                            ["@odata.type"] = "#microsoft.graph.toneInfo",
                            ["tone"]        = graphTone,
                            ["sequenceId"]  = 1
                        }
                    }
                }
            )
        };
        return envelope.ToJsonString();
    }

    private static string BuildPlayPromptOperationNotification(string callId, string operationId)
    {
        var envelope = new JsonObject
        {
            ["@odata.type"] = "#microsoft.graph.commsNotifications",
            ["value"] = new JsonArray(
                new JsonObject
                {
                    ["@odata.type"]  = "#microsoft.graph.commsNotification",
                    ["changeType"]   = "updated",
                    ["resourceUrl"]  = $"/communications/calls/{callId}/operations/{operationId}",
                    ["resourceData"] = new JsonObject
                    {
                        ["@odata.type"]   = "#microsoft.graph.playPromptOperation",
                        ["id"]            = operationId,
                        ["status"]        = "completed",
                        ["clientContext"] = callId
                    }
                }
            )
        };
        return envelope.ToJsonString();
    }

    /// <summary>
    /// Build a recordOperation completed notification.
    /// The transcript is embedded in clientContext so the bot can inject it into
    /// CallLog.Metadata[MetaPendingSpeech] before calling TranscriptRoutingService.
    /// Format: "callId|transcript text"
    /// </summary>
    private static string BuildRecordOperationNotification(
        string callId, string operationId, string transcript)
    {
        var envelope = new JsonObject
        {
            ["@odata.type"] = "#microsoft.graph.commsNotifications",
            ["value"] = new JsonArray(
                new JsonObject
                {
                    ["@odata.type"]  = "#microsoft.graph.commsNotification",
                    ["changeType"]   = "updated",
                    ["resourceUrl"]  = $"/communications/calls/{callId}/operations/{operationId}",
                    ["resourceData"] = new JsonObject
                    {
                        ["@odata.type"]   = "#microsoft.graph.recordOperation",
                        ["id"]            = operationId,
                        ["status"]        = "completed",
                        ["clientContext"] = $"{callId}|{transcript}",
                        ["resultInfo"]    = new JsonObject
                        {
                            ["code"]    = 200,
                            ["subCode"] = 8541,
                            ["message"] = "Stop tone received."
                        }
                    }
                }
            )
        };
        return envelope.ToJsonString();
    }

    // ════════════════════════════════════════════════════════════
    //  HTTP helpers
    // ════════════════════════════════════════════════════════════

    private async Task<bool> PostToBotEndpointAsync(
        string ivrEndpoint, string payload, SimulatedCall call, string description)
    {
        try
        {
            var result = await PostToBotEndpointRawAsync(ivrEndpoint, payload);
            _callManager.UpdateCall(call.Id, c => c.AddEvent(
                CallEventSource.Acs,
                result ? $"{description} → IVR accepted" : $"{description} → IVR rejected"));
            return result;
        }
        catch (Exception ex)
        {
            _callManager.TransitionCall(call.Id, CallState.Failed, CallEventSource.System,
                $"Failed to reach IVR: {description}", ex.Message);
            return false;
        }
    }

    private async Task<bool> PostToBotEndpointRawAsync(string ivrEndpoint, string payload)
    {
        var client = _httpClientFactory.CreateClient();
        // Teams sends these without auth in the simulator (no JWT validation in dev)
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{ivrEndpoint}/api/bot-messages", content);
        _logger.LogDebug("Bot endpoint response: HTTP {Status}", (int)response.StatusCode);
        return response.IsSuccessStatusCode;
    }

    // ════════════════════════════════════════════════════════════
    //  Utilities
    // ════════════════════════════════════════════════════════════

    private static string? ExtractCallId(string path)
    {
        // communications/calls/{callId}[/action]
        var parts = path.TrimStart('/').Split('/');
        var idx   = Array.IndexOf(parts, "calls");
        return idx >= 0 && idx + 1 < parts.Length ? parts[idx + 1] : null;
    }

    private static string MapDigitToGraphTone(string digit) => digit switch
    {
        "0" => "tone0",
        "1" => "tone1",
        "2" => "tone2",
        "3" => "tone3",
        "4" => "tone4",
        "5" => "tone5",
        "6" => "tone6",
        "7" => "tone7",
        "8" => "tone8",
        "9" => "tone9",
        "#" => "pound",
        "*" => "star",
        _   => digit.ToLowerInvariant()
    };
}
