using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Mock ACS implementation — runs a fake ACS Call Automation REST API and
/// manages the call lifecycle without any real Azure resources.
///
/// The IVR Functions connect to this mock by setting their ACS connection string
/// to: endpoint=http://pstn-simulator:8080;accesskey=bW9ja2tleQ==
///
/// Handles:
///   POST /calling/callConnections:answer          → AnswerCallAsync
///   POST /calling/callConnections/{id}:play       → PlayToAllAsync
///   POST /calling/callConnections/{id}:recognize  → StartRecognizingAsync
///   POST /calling/callConnections/{id}:transferToParticipant → TransferCallAsync
///   POST /calling/callConnections/{id}:hangUp     → HangUpAsync
///   DELETE /calling/callConnections/{id}          → HangUpAsync
/// </summary>
public class MockAcsBridge : IAcsBridge
{
    private readonly CallStateManager _callManager;
    private readonly Cm10Service _cm10;
    private readonly PstnService _pstn;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MockAcsBridge> _logger;

    public MockAcsBridge(
        CallStateManager callManager,
        Cm10Service cm10,
        PstnService pstn,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<MockAcsBridge> logger)
    {
        _callManager = callManager;
        _cm10 = cm10;
        _pstn = pstn;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════
    //  IAcsBridge — Originate and interact with calls
    // ════════════════════════════════════════════════════════════

    public async Task<bool> OriginateCallAsync(SimulatedCall call)
    {
        var ivrEndpoint = _config["IvrEndpoint"] ?? "http://localhost:7071";

        // Build the EventGrid IncomingCall event
        var incomingCallContext = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                callId = call.Id,
                callerNumber = call.CallerNumber,
                calledNumber = call.DidNumber
            })));

        var eventGridPayload = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid().ToString(),
                topic = "/subscriptions/mock/resourceGroups/mock/providers/Microsoft.Communication/CommunicationServices/pstn-simulator",
                subject = $"/calling/callConnections/{call.Id}",
                data = new
                {
                    to = new { rawId = $"4:{call.DidNumber}", phoneNumber = new { value = call.DidNumber } },
                    from = new { rawId = $"4:{call.CallerNumber}", phoneNumber = new { value = call.CallerNumber } },
                    serverCallId = Guid.NewGuid().ToString(),
                    callerDisplayName = call.CallerName,
                    incomingCallContext = incomingCallContext,
                    correlationId = call.Id
                },
                eventType = "Microsoft.Communication.IncomingCall",
                dataVersion = "1.0",
                metadataVersion = "1",
                eventTime = DateTime.UtcNow.ToString("o")
            }
        });

        call.TransitionTo(CallState.SentToIvr, CallEventSource.Acs,
            "EventGrid IncomingCall sent to IVR",
            $"POST {ivrEndpoint}/api/incoming-call");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(eventGridPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ivrEndpoint}/api/incoming-call", content);

            if (response.IsSuccessStatusCode)
            {
                call.AddEvent(CallEventSource.Ivr, "IVR accepted IncomingCall event",
                    $"HTTP {(int)response.StatusCode}");
                return true;
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                call.TransitionTo(CallState.Failed, CallEventSource.Ivr,
                    $"IVR rejected event: HTTP {(int)response.StatusCode}", body);
                return false;
            }
        }
        catch (Exception ex)
        {
            call.TransitionTo(CallState.Failed, CallEventSource.System,
                "Failed to reach IVR endpoint", ex.Message);
            return false;
        }
    }

    public async Task SendDtmfAsync(string callId, string tone)
    {
        var call = _callManager.GetCall(callId);
        if (call?.CallbackUri == null || call.CallConnectionId == null) return;

        call.DtmfInputs.Add(tone);
        call.AddEvent(CallEventSource.Pstn, $"DTMF tone: {tone}");

        var dtmfToneName = MapDtmfTone(tone);
        await SendCallbackAsync(call.CallbackUri, call.CallConnectionId, call.ServerCallId!,
            "Microsoft.Communication.RecognizeCompleted",
            new
            {
                callConnectionId = call.CallConnectionId,
                serverCallId = call.ServerCallId,
                correlationId = call.Id,
                resultInformation = new { code = 200, subCode = 0, message = "Action completed successfully." },
                recognitionType = "dtmf",
                dtmfResult = new { tones = new[] { dtmfToneName } }
            });

        // Don't overwrite state — the IVR callback already triggered a new
        // Recognize/Play/Transfer that set a more advanced state.
        call.AddEvent(CallEventSource.Acs, $"RecognizeCompleted (DTMF: {tone}) sent to IVR");
    }

    public async Task SendSpeechAsync(string callId, string text)
    {
        var call = _callManager.GetCall(callId);
        if (call?.CallbackUri == null || call.CallConnectionId == null) return;

        call.SpeechInputs.Add(text);
        call.AddEvent(CallEventSource.Pstn, $"Speech: \"{text}\"");

        await SendCallbackAsync(call.CallbackUri, call.CallConnectionId, call.ServerCallId!,
            "Microsoft.Communication.RecognizeCompleted",
            new
            {
                callConnectionId = call.CallConnectionId,
                serverCallId = call.ServerCallId,
                correlationId = call.Id,
                resultInformation = new { code = 200, subCode = 0, message = "Action completed successfully." },
                recognitionType = "speech",
                speechResult = new { speech = text }
            });

        // Don't overwrite state — the IVR callback already triggered a new
        // Recognize/Play/Transfer that set a more advanced state.
        call.AddEvent(CallEventSource.Acs, $"RecognizeCompleted (Speech) sent to IVR");
    }

    public async Task DisconnectAsync(string callId)
    {
        var call = _callManager.GetCall(callId);
        if (call?.CallbackUri == null || call.CallConnectionId == null) return;

        await SendCallbackAsync(call.CallbackUri, call.CallConnectionId, call.ServerCallId!,
            "Microsoft.Communication.CallDisconnected",
            new
            {
                callConnectionId = call.CallConnectionId,
                serverCallId = call.ServerCallId,
                correlationId = call.Id
            });

        // Release trunk
        if (call.TrunkName != null) _pstn.ReleaseChannel(call.TrunkName);

        // Release agent
        if (call.AssignedAgentId != null) _cm10.AgentReleased(call.AssignedAgentId);

        _callManager.CompleteCall(callId, CallState.Disconnected, CallEventSource.Pstn,
            "Caller disconnected");
    }

    // ════════════════════════════════════════════════════════════
    //  Mock ACS REST API — handles SDK HTTP requests from IVR
    // ════════════════════════════════════════════════════════════

    public async Task<IResult> HandleAcsApiAsync(HttpContext ctx, string path)
    {
        var method = ctx.Request.Method;
        string body;
        using (var reader = new StreamReader(ctx.Request.Body))
            body = await reader.ReadToEndAsync();

        _logger.LogInformation("Mock ACS: {Method} /calling/{Path}", method, path);

        // ─── Answer Call ────────────────────────────────────
        if (path.StartsWith("callConnections:answer", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleAnswerAsync(body);
        }

        // ─── Play ───────────────────────────────────────────
        var playMatch = Regex.Match(path, @"callConnections/([^:]+):play");
        if (playMatch.Success)
        {
            return await HandlePlayAsync(playMatch.Groups[1].Value, body);
        }

        // ─── Recognize ──────────────────────────────────────
        var recognizeMatch = Regex.Match(path, @"callConnections/([^:]+):recognize");
        if (recognizeMatch.Success)
        {
            return await HandleRecognizeAsync(recognizeMatch.Groups[1].Value, body);
        }

        // ─── Transfer ───────────────────────────────────────
        var transferMatch = Regex.Match(path, @"callConnections/([^:]+):transferToParticipant");
        if (transferMatch.Success)
        {
            return await HandleTransferAsync(transferMatch.Groups[1].Value, body);
        }

        // ─── Hang Up (POST :hangUp or DELETE) ───────────────
        var hangUpMatch = Regex.Match(path, @"callConnections/([^:?]+)(?::hangUp)?$");
        if (hangUpMatch.Success && (method == "DELETE" || path.Contains(":hangUp")))
        {
            return await HandleHangUpAsync(hangUpMatch.Groups[1].Value);
        }

        _logger.LogWarning("Mock ACS: Unhandled route — {Method} /calling/{Path}", method, path);
        return Results.NotFound();
    }

    // ─── Answer ─────────────────────────────────────────────

    private async Task<IResult> HandleAnswerAsync(string body)
    {
        try
        {
            var json = JsonDocument.Parse(body);
            var incomingCallContext = json.RootElement.GetProperty("incomingCallContext").GetString()!;
            var callbackUri = json.RootElement.GetProperty("callbackUri").GetString()!;

            // Decode embedded call context
            var contextJson = Encoding.UTF8.GetString(Convert.FromBase64String(incomingCallContext));
            var context = JsonDocument.Parse(contextJson);
            var callId = context.RootElement.GetProperty("callId").GetString()!;

            var connectionId = Guid.NewGuid().ToString();
            var serverCallId = Guid.NewGuid().ToString();

            // Register the connection
            _callManager.RegisterConnection(callId, connectionId, callbackUri, serverCallId);
            _callManager.TransitionCall(callId, CallState.IvrAnswered, CallEventSource.Ivr,
                "IVR answered call", $"connectionId={connectionId}");

            // Send CallConnected callback after a brief delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                await SendCallbackAsync(callbackUri, connectionId, serverCallId,
                    "Microsoft.Communication.CallConnected",
                    new
                    {
                        callConnectionId = connectionId,
                        serverCallId = serverCallId,
                        correlationId = callId
                    });

                // Only log — do NOT overwrite state here.
                // By the time the callback round-trips, the IVR has already
                // sent a Recognize/Play request that set a more advanced state.
                var current = _callManager.GetCall(callId);
                current?.AddEvent(CallEventSource.Acs, "CallConnected callback sent to IVR");
            });

            // Return ACS-compatible answer response
            var response = new
            {
                callConnectionId = connectionId,
                serverCallId = serverCallId,
                callConnectionState = "connected",
                callbackUri = callbackUri,
                source = new { rawId = "mock-source", kind = "communicationUser", communicationUser = new { id = "mock-source" } },
                targets = Array.Empty<object>()
            };

            _logger.LogInformation("Mock ACS: Answered call {CallId} → connection {ConnectionId}", callId, connectionId);
            return Results.Json(response, statusCode: 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mock ACS: Failed to handle AnswerCall");
            return Results.Problem(ex.Message);
        }
    }

    // ─── Play ───────────────────────────────────────────────

    private async Task<IResult> HandlePlayAsync(string connectionId, string body)
    {
        var call = _callManager.GetCallByConnectionId(connectionId);
        if (call == null) return Results.NotFound();

        // Extract prompt content from request
        string promptText = "(unknown prompt)";
        try
        {
            var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("playSources", out var sources))
            {
                var firstSource = sources.EnumerateArray().FirstOrDefault();
                promptText = ExtractPlaySourceText(firstSource) ?? promptText;
            }

            // Log body for debugging if extraction failed
            if (promptText == "(unknown prompt)")
                _logger.LogDebug("Play: Could not extract prompt. Body: {Body}", body);
        }
        catch { /* best effort parsing */ }

        call.CurrentPromptText = promptText;
        _callManager.TransitionCall(call.Id, CallState.IvrPlaying, CallEventSource.Ivr,
            $"Playing prompt: {promptText[..Math.Min(80, promptText.Length)]}");

        // Send PlayCompleted callback after a simulated delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500); // Simulate prompt playback time
            if (call.CallbackUri != null)
            {
                await SendCallbackAsync(call.CallbackUri, connectionId, call.ServerCallId!,
                    "Microsoft.Communication.PlayCompleted",
                    new
                    {
                        callConnectionId = connectionId,
                        serverCallId = call.ServerCallId,
                        correlationId = call.Id,
                        resultInformation = new { code = 200, subCode = 0, message = "Action completed successfully." }
                    });
            }
        });

        return Results.Json(new { }, statusCode: 202);
    }

    // ─── Recognize ──────────────────────────────────────────

    private Task<IResult> HandleRecognizeAsync(string connectionId, string body)
    {
        var call = _callManager.GetCallByConnectionId(connectionId);
        if (call == null) return Task.FromResult<IResult>(Results.NotFound());

        // Determine recognition type and extract prompt text
        string recognizeType = "dtmf";
        string? promptText = null;
        try
        {
            var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("recognizeInputType", out var inputType))
                recognizeType = inputType.GetString() ?? "dtmf";

            // Extract prompt text from the playPrompt property (ACS SDK sends prompt with recognize)
            promptText = ExtractRecognizePrompt(json.RootElement);

            if (promptText == null)
                _logger.LogDebug("Mock ACS Recognize: No prompt text found. Body keys: {Keys}",
                    string.Join(", ", json.RootElement.EnumerateObject().Select(p => p.Name)));
        }
        catch { /* best effort */ }

        if (!string.IsNullOrEmpty(promptText))
            call.CurrentPromptText = promptText;

        call.RecognizeType = recognizeType;
        var promptPreview = promptText != null
            ? $" — \"{promptText[..Math.Min(80, promptText.Length)]}\""
            : "";
        _callManager.TransitionCall(call.Id, CallState.IvrRecognizing, CallEventSource.Ivr,
            $"Waiting for {recognizeType} input{promptPreview}",
            recognizeType == "dtmf" ? "Press a DTMF key on the dialer" : "Type speech text and submit");

        // NOTE: We do NOT auto-send a callback here.
        // The user will trigger it via SendDtmfAsync or SendSpeechAsync from the UI.

        return Task.FromResult<IResult>(Results.Json(new { }, statusCode: 202));
    }

    /// <summary>
    /// Extract prompt text from the playPrompt property in a recognize request.
    /// The ACS SDK serializes the prompt in various nested formats.
    /// </summary>
    private static string? ExtractRecognizePrompt(JsonElement root)
    {
        if (!root.TryGetProperty("playPrompt", out var playPrompt))
            return null;

        // Format 1: playPrompt.text (flat text string)
        if (playPrompt.TryGetProperty("text", out var textProp))
        {
            // Could be a string directly or a nested object { text: "...", voiceName: "..." }
            if (textProp.ValueKind == JsonValueKind.String)
                return textProp.GetString();
            if (textProp.ValueKind == JsonValueKind.Object && textProp.TryGetProperty("text", out var innerText))
                return innerText.GetString();
        }

        // Format 2: playPrompt.kind → then read source by kind
        if (playPrompt.TryGetProperty("kind", out var kind))
        {
            var kindStr = kind.GetString();
            if (kindStr == "textSource" && playPrompt.TryGetProperty("textSource", out var ts))
            {
                if (ts.TryGetProperty("text", out var tsText))
                    return tsText.GetString();
            }
            if (kindStr == "ssmlSource" && playPrompt.TryGetProperty("ssmlSource", out var ss))
            {
                if (ss.TryGetProperty("ssmlText", out var ssml))
                    return $"[SSML] {ssml.GetString()}";
            }
            if (kindStr == "text" && playPrompt.TryGetProperty("text", out var t2))
            {
                if (t2.ValueKind == JsonValueKind.Object && t2.TryGetProperty("text", out var t2Inner))
                    return t2Inner.GetString();
            }
        }

        // Format 3: playPrompt.playSources[0]... (array of sources)
        if (playPrompt.TryGetProperty("playSources", out var sources) && sources.GetArrayLength() > 0)
        {
            var first = sources.EnumerateArray().First();
            if (first.TryGetProperty("text", out var srcText))
            {
                if (srcText.ValueKind == JsonValueKind.String)
                    return srcText.GetString();
                if (srcText.TryGetProperty("text", out var nested))
                    return nested.GetString();
            }
        }

        return null;
    }

    /// <summary>
    /// Extract text from a single playSource element (used in :play requests).
    /// The ACS SDK serializes TextSource with various nested formats.
    /// </summary>
    private static string? ExtractPlaySourceText(JsonElement source)
    {
        // Flat: { "text": "Hello" }
        if (source.TryGetProperty("text", out var text))
        {
            if (text.ValueKind == JsonValueKind.String)
                return text.GetString();
            if (text.ValueKind == JsonValueKind.Object && text.TryGetProperty("text", out var inner))
                return inner.GetString();
        }

        // Nested by kind: { "kind": "textSource", "textSource": { "text": "Hello" } }
        if (source.TryGetProperty("kind", out var kind))
        {
            var kindStr = kind.GetString();
            if (kindStr == "textSource" && source.TryGetProperty("textSource", out var ts))
            {
                if (ts.TryGetProperty("text", out var tsText))
                    return tsText.GetString();
            }
            if (kindStr == "text" && source.TryGetProperty("text", out var t2))
            {
                if (t2.ValueKind == JsonValueKind.Object && t2.TryGetProperty("text", out var t2Inner))
                    return t2Inner.GetString();
            }
            if (kindStr == "ssmlSource" && source.TryGetProperty("ssmlSource", out var ss))
            {
                if (ss.TryGetProperty("ssmlText", out var ssml))
                    return $"[SSML] {ssml.GetString()}";
            }
        }

        // SSML / URI fallbacks
        if (source.TryGetProperty("ssml", out var ssmlFlat))
            return $"[SSML] {ssmlFlat.GetString()}";
        if (source.TryGetProperty("uri", out var uri))
            return $"[Audio] {uri.GetString()}";

        return null;
    }

    // ─── Transfer ───────────────────────────────────────────

    private async Task<IResult> HandleTransferAsync(string connectionId, string body)
    {
        var call = _callManager.GetCallByConnectionId(connectionId);
        if (call == null) return Results.NotFound();

        // Extract transfer target
        string targetAddress = "unknown";
        try
        {
            var json = JsonDocument.Parse(body);
            var target = json.RootElement.GetProperty("targetParticipant");

            if (target.TryGetProperty("phoneNumber", out var phone))
                targetAddress = phone.GetProperty("value").GetString() ?? targetAddress;
            else if (target.TryGetProperty("rawId", out var rawId))
                targetAddress = rawId.GetString() ?? targetAddress;
            else if (target.TryGetProperty("communicationUser", out var user))
                targetAddress = user.GetProperty("id").GetString() ?? targetAddress;
        }
        catch { /* best effort */ }

        _callManager.TransitionCall(call.Id, CallState.IvrTransfer, CallEventSource.Ivr,
            $"IVR transferring call to: {targetAddress}");

        // Route the transfer back through CM10
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);

            var routingResult = _cm10.HandleTransferToVdn(targetAddress);

            if (routingResult.Success && routingResult.Destination != null)
            {
                call.AddEvent(CallEventSource.Cm10,
                    $"CM10 received transfer → VDN {routingResult.VdnNumber} ({routingResult.VdnLabel})",
                    $"Destination: {routingResult.Destination}");

                if (routingResult.Destination.StartsWith("Queue:"))
                {
                    var queueName = routingResult.Destination[6..];
                    _callManager.TransitionCall(call.Id, CallState.AcdQueued, CallEventSource.Cm10,
                        $"Call queued in {queueName}");

                    // Try to assign an agent
                    var assignment = _cm10.AssignAgent(queueName);
                    if (assignment != null)
                    {
                        call.QueueName = queueName;
                        call.AssignedAgentId = assignment.AgentId;
                        call.AssignedAgentName = assignment.AgentName;

                        _callManager.TransitionCall(call.Id, CallState.AgentRinging, CallEventSource.Cm10,
                            $"Ringing agent {assignment.AgentName} (ext {assignment.Extension})");

                        // Simulate agent answer after a delay
                        await Task.Delay(2000);
                        _cm10.AgentAnswered(assignment.AgentId, call.Id);
                        _callManager.TransitionCall(call.Id, CallState.AgentConnected, CallEventSource.Agent,
                            $"Agent {assignment.AgentName} answered — caller connected");
                    }
                    else
                    {
                        call.QueueName = queueName;
                        call.AddEvent(CallEventSource.Cm10,
                            "No agents available — call waiting in queue");
                    }
                }
                else
                {
                    call.AddEvent(CallEventSource.Cm10,
                        $"Routed to destination: {routingResult.Destination}");
                }
            }
            else
            {
                call.AddEvent(CallEventSource.Cm10,
                    $"Transfer target not found: {targetAddress}", routingResult.Error);
            }
        });

        return Results.Json(new { operationContext = "" }, statusCode: 202);
    }

    // ─── Hang Up ────────────────────────────────────────────

    private Task<IResult> HandleHangUpAsync(string connectionId)
    {
        var call = _callManager.GetCallByConnectionId(connectionId);
        if (call == null) return Task.FromResult<IResult>(Results.NoContent());

        // Release resources
        if (call.TrunkName != null) _pstn.ReleaseChannel(call.TrunkName);
        if (call.AssignedAgentId != null) _cm10.AgentReleased(call.AssignedAgentId);

        _callManager.CompleteCall(call.Id, CallState.Disconnected, CallEventSource.Ivr,
            "IVR hung up the call");

        return Task.FromResult<IResult>(Results.NoContent());
    }

    // ════════════════════════════════════════════════════════════
    //  Callback Helpers
    // ════════════════════════════════════════════════════════════

    private async Task SendCallbackAsync(string callbackUri, string connectionId, string serverCallId,
        string eventType, object data)
    {
        var cloudEvents = new[]
        {
            new
            {
                id = Guid.NewGuid().ToString(),
                source = $"calling/callConnections/{connectionId}",
                type = eventType,
                data = data,
                time = DateTime.UtcNow.ToString("o"),
                specversion = "1.0",
                datacontenttype = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(cloudEvents);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(callbackUri, content);

            _logger.LogInformation("Callback {EventType} → {Uri} → HTTP {Status}",
                eventType.Split('.').Last(), callbackUri, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send callback {EventType} to {Uri}", eventType, callbackUri);
        }
    }

    private static string MapDtmfTone(string key) => key switch
    {
        "0" => "Zero", "1" => "One", "2" => "Two", "3" => "Three",
        "4" => "Four", "5" => "Five", "6" => "Six", "7" => "Seven",
        "8" => "Eight", "9" => "Nine", "*" => "Asterisk", "#" => "Pound",
        _ => key
    };
}
