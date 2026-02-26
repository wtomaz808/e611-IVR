using Azure.Communication;
using Azure.Communication.CallAutomation;
using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Live ACS implementation — uses a real Azure Communication Services instance
/// to originate calls. The IVR receives real IncomingCall events from ACS.
///
/// Requirements:
///   - AcsConnectionString: valid ACS connection string
///   - AcsCallerNumber: a phone number purchased in ACS (caller ID)
///
/// This mode incurs real ACS per-minute charges.
/// </summary>
public class LiveAcsBridge : IAcsBridge
{
    private readonly CallAutomationClient? _client;
    private readonly CallStateManager _callManager;
    private readonly Cm10Service _cm10;
    private readonly PstnService _pstn;
    private readonly string? _callerNumber;
    private readonly ILogger<LiveAcsBridge> _logger;

    public LiveAcsBridge(
        CallStateManager callManager,
        Cm10Service cm10,
        PstnService pstn,
        IConfiguration config,
        ILogger<LiveAcsBridge> logger)
    {
        _callManager = callManager;
        _cm10 = cm10;
        _pstn = pstn;
        _logger = logger;

        var acsConnectionString = config["AcsConnectionString"];
        _callerNumber = config["AcsCallerNumber"];

        if (!string.IsNullOrEmpty(acsConnectionString))
        {
            _client = new CallAutomationClient(acsConnectionString);
            _logger.LogInformation("Live ACS bridge initialized with real connection string");
        }
        else
        {
            _logger.LogWarning("Live ACS bridge: No AcsConnectionString configured. Live calls will fail.");
        }
    }

    public async Task<bool> OriginateCallAsync(SimulatedCall call)
    {
        if (_client == null || string.IsNullOrEmpty(_callerNumber))
        {
            call.TransitionTo(CallState.Failed, CallEventSource.System,
                "Live ACS not configured — set AcsConnectionString and AcsCallerNumber");
            return false;
        }

        try
        {
            call.TransitionTo(CallState.SentToIvr, CallEventSource.Acs,
                "Originating real ACS call",
                $"From: {_callerNumber} → To: {call.DidNumber}");

            // CreateCall originates an outbound call. The target DID should route back
            // through Direct Routing to your IVR, triggering a real IncomingCall event.
            var target = new PhoneNumberIdentifier(call.DidNumber);
            var callbackUri = new Uri($"http://localhost:8080/api/live-callbacks/{call.Id}");

            var callInvite = new CallInvite(target, new PhoneNumberIdentifier(_callerNumber));
            callInvite.SourceDisplayName = call.CallerName;

            var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
            var result = await _client.CreateCallAsync(createCallOptions);

            var connectionId = result.Value.CallConnection.CallConnectionId;
            call.CallConnectionId = connectionId;
            call.ServerCallId = result.Value.CallConnectionProperties.ServerCallId;

            _callManager.TransitionCall(call.Id, CallState.IvrAnswered, CallEventSource.Acs,
                $"Real ACS call created — connection {connectionId}");

            return true;
        }
        catch (Exception ex)
        {
            call.TransitionTo(CallState.Failed, CallEventSource.System,
                "Failed to create live ACS call", ex.Message);
            _logger.LogError(ex, "Live ACS: CreateCallAsync failed for {CallId}", call.Id);
            return false;
        }
    }

    public async Task SendDtmfAsync(string callId, string tone)
    {
        if (_client == null) return;

        var call = _callManager.GetCall(callId);
        if (call?.CallConnectionId == null) return;

        try
        {
            var callConnection = _client.GetCallConnection(call.CallConnectionId);
            var dtmfTone = MapTone(tone);
            if (dtmfTone.HasValue)
            {
                await callConnection.GetCallMedia().SendDtmfTonesAsync(
                    new[] { dtmfTone.Value },
                    new PhoneNumberIdentifier(call.DidNumber));

                call.DtmfInputs.Add(tone);
                call.AddEvent(CallEventSource.Pstn, $"Sent real DTMF tone: {tone}");
                _callManager.TransitionCall(callId, CallState.IvrPlaying, CallEventSource.Acs,
                    "DTMF sent via real ACS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live ACS: SendDtmf failed for {CallId}", callId);
            call.AddEvent(CallEventSource.System, "Failed to send DTMF", ex.Message);
        }
    }

    public Task SendSpeechAsync(string callId, string text)
    {
        // Cannot send speech programmatically in Live mode — would need a real microphone
        var call = _callManager.GetCall(callId);
        call?.AddEvent(CallEventSource.System,
            "Speech input not supported in Live mode — use a real phone to speak");
        return Task.CompletedTask;
    }

    public async Task DisconnectAsync(string callId)
    {
        if (_client == null) return;

        var call = _callManager.GetCall(callId);
        if (call?.CallConnectionId == null) return;

        try
        {
            var callConnection = _client.GetCallConnection(call.CallConnectionId);
            await callConnection.HangUpAsync(true);
            _callManager.CompleteCall(callId, CallState.Disconnected, CallEventSource.Pstn,
                "Caller disconnected (real ACS)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live ACS: HangUp failed for {CallId}", callId);
        }

        if (call.TrunkName != null) _pstn.ReleaseChannel(call.TrunkName);
        if (call.AssignedAgentId != null) _cm10.AgentReleased(call.AssignedAgentId);
    }

    private static DtmfTone? MapTone(string key) => key switch
    {
        "0" => DtmfTone.Zero, "1" => DtmfTone.One, "2" => DtmfTone.Two,
        "3" => DtmfTone.Three, "4" => DtmfTone.Four, "5" => DtmfTone.Five,
        "6" => DtmfTone.Six, "7" => DtmfTone.Seven, "8" => DtmfTone.Eight,
        "9" => DtmfTone.Nine, "#" => DtmfTone.Pound, "*" => DtmfTone.Asterisk,
        _ => null
    };
}
