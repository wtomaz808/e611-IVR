using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Bridge interface between the simulator and Azure Communication Services.
/// Two implementations: MockAcsBridge (offline) and LiveAcsBridge (real ACS).
/// </summary>
public interface IAcsBridge
{
    /// <summary>
    /// Originate a call — send the IncomingCall event to the IVR and manage the call lifecycle.
    /// </summary>
    Task<bool> OriginateCallAsync(SimulatedCall call);

    /// <summary>
    /// Send a DTMF tone from the "caller" to the IVR.
    /// In Mock mode, this sends a RecognizeCompleted callback.
    /// In Live mode, this sends DTMF via ACS SDK.
    /// </summary>
    Task SendDtmfAsync(string callId, string tone);

    /// <summary>
    /// Send a speech transcription from the "caller" to the IVR.
    /// In Mock mode, this sends a RecognizeCompleted callback with speech result.
    /// </summary>
    Task SendSpeechAsync(string callId, string text);

    /// <summary>
    /// Disconnect the call from the caller side.
    /// </summary>
    Task DisconnectAsync(string callId);
}
