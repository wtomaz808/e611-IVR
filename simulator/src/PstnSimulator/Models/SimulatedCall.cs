namespace PstnSimulator.Models;

// ════════════════════════════════════════════════════════════
//  Call State Machine
// ════════════════════════════════════════════════════════════

public enum CallState
{
    Initiated,      // User clicked "Call" — not yet on trunk
    TrunkSeized,    // Channel allocated on trunk
    VdnRouting,     // CM10 evaluating VDN vector
    SentToIvr,      // EventGrid IncomingCall sent to IVR
    IvrAnswered,    // IVR called AnswerCallAsync
    IvrPlaying,     // IVR playing a prompt
    IvrRecognizing, // IVR waiting for DTMF/speech
    IvrTransfer,    // IVR transferring call
    AcdQueued,      // Call queued in CM10 ACD
    AgentRinging,   // Agent phone ringing
    AgentConnected, // Caller talking to agent
    Disconnected,   // Call ended
    Failed          // Something went wrong
}

public enum CallEventSource { Pstn, Cm10, Acs, Ivr, Agent, System }

/// <summary>
/// A timestamped event in the call's lifecycle.
/// </summary>
public class CallEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public CallEventSource Source { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

/// <summary>
/// Represents a single simulated call flowing through PSTN → CM10 → IVR → Agent.
/// </summary>
public class SimulatedCall
{
    public string Id { get; set; } = $"sim-{Guid.NewGuid().ToString("N")[..8]}";
    public CallState State { get; set; } = CallState.Initiated;

    // ─── Parties ────────────────────────────────────────────
    public string CallerNumber { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public CallerType CallerType { get; set; } = CallerType.Regular;
    public string DidNumber { get; set; } = string.Empty;
    public string DidLabel { get; set; } = string.Empty;

    // ─── PSTN ───────────────────────────────────────────────
    public string? TrunkName { get; set; }
    public int? TrunkChannel { get; set; }
    public string? GatewayName { get; set; }

    // ─── CM10 ───────────────────────────────────────────────
    public string? VdnNumber { get; set; }
    public string? VdnLabel { get; set; }
    public string? VectorId { get; set; }

    // ─── ACS / IVR ──────────────────────────────────────────
    public string? CallConnectionId { get; set; }
    public string? ServerCallId { get; set; }
    public string? CallbackUri { get; set; }
    public string? CurrentPromptText { get; set; }
    public string? RecognizeType { get; set; } // "dtmf" or "speech"
    public int? MaxDtmfTones { get; set; }

    // ─── ACD / Agent ────────────────────────────────────────
    public string? QueueName { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }

    // ─── Timing ─────────────────────────────────────────────
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? AnswerTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? DurationSeconds => EndTime.HasValue
        ? (EndTime.Value - StartTime).TotalSeconds
        : (DateTime.UtcNow - StartTime).TotalSeconds;

    // ─── Event Trace ────────────────────────────────────────
    public List<CallEvent> Events { get; set; } = new();

    // ─── Inputs captured ────────────────────────────────────
    public List<string> DtmfInputs { get; set; } = new();
    public List<string> SpeechInputs { get; set; } = new();

    public SimulatedCall()
    {
        Id = $"sim-{Guid.NewGuid().ToString("N")[..8]}";
    }

    public void AddEvent(CallEventSource source, string message, string? detail = null)
    {
        Events.Add(new CallEvent
        {
            Timestamp = DateTime.UtcNow,
            Source = source,
            Message = message,
            Detail = detail
        });
    }

    public void TransitionTo(CallState newState, CallEventSource source, string message, string? detail = null)
    {
        State = newState;
        AddEvent(source, message, detail);
    }
}

/// <summary>
/// Completed call record for history/reporting.
/// </summary>
public class CallDetailRecord
{
    public string CallId { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string DidNumber { get; set; } = string.Empty;
    public string? VdnNumber { get; set; }
    public string? TrunkName { get; set; }
    public string? QueueName { get; set; }
    public string? AgentName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? DurationSeconds { get; set; }
    public TimeSpan Duration => EndTime.HasValue
        ? EndTime.Value - StartTime
        : TimeSpan.Zero;
    public CallState FinalState { get; set; }
    public int EventCount { get; set; }
    public List<string> DtmfInputs { get; set; } = new();
}
