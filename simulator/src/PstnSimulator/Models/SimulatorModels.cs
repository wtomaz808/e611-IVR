namespace PstnSimulator.Models;

// ════════════════════════════════════════════════════════════
//  PSTN Layer Models
// ════════════════════════════════════════════════════════════

public enum TrunkType { PRI, SIP }
public enum CallerType { Regular, VIP, Blocked }
public enum GatewayType { ASBCE, SessionManager, AudioCodes, Ribbon, Oracle }
public enum AgentState { Available, Busy, OnBreak, Offline, Ringing, OnCall }
public enum QueueStrategy { RoundRobin, MostIdle, Linear }
public enum VectorStepType { Route, Queue, Announcement, Wait, Goto, TimeCheck, Disconnect }

/// <summary>
/// An outbound gateway/SBC — routes calls from CM10 to ACS.
/// This is the egress device that CM10 sends calls through after VDN/vector processing.
/// Typically an Avaya ASBCE, Session Manager, or third-party SBC (AudioCodes, Ribbon, Oracle).
/// </summary>
public class GatewayConfig
{
    public string Name { get; set; } = string.Empty;
    public GatewayType Type { get; set; } = GatewayType.ASBCE;
    public string Fqdn { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int SipPort { get; set; } = 5061;
    public bool Registered { get; set; } = true;
    public bool TlsEnabled { get; set; } = true;

    /// <summary>Trunk names that route through this gateway</summary>
    public List<string> TrunkNames { get; set; } = new();
}

/// <summary>
/// A simulated PSTN trunk (carrier inbound or outbound to gateway/SBC).
/// </summary>
public class TrunkConfig
{
    public string Name { get; set; } = string.Empty;
    public TrunkType Type { get; set; } = TrunkType.PRI;
    public int Capacity { get; set; } = 23;
    public int ActiveChannels { get; set; }
    public string? GatewayName { get; set; }

    public bool HasCapacity => ActiveChannels < Capacity;
}

/// <summary>
/// A phone number (DID) that routes into the system via a trunk.
/// </summary>
public class SimulatedDid
{
    public string Number { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string TrunkName { get; set; } = string.Empty;
    public string VdnNumber { get; set; } = string.Empty;
}

/// <summary>
/// A simulated caller with identity and type.
/// </summary>
public class SimulatedCaller
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CallerType Type { get; set; } = CallerType.Regular;
}

// ════════════════════════════════════════════════════════════
//  CM10 Models
// ════════════════════════════════════════════════════════════

/// <summary>
/// An Avaya CM10 Vector Directory Number — the inbound entry point.
/// </summary>
public class Vdn
{
    public string Number { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string VectorId { get; set; } = string.Empty;

    /// <summary>
    /// Destination type: "IVR", "Queue:queue-name", "Announcement:id", "VDN:number"
    /// </summary>
    public string Destination { get; set; } = string.Empty;
}

/// <summary>
/// A single step in a CM10 call vector.
/// </summary>
public class VectorStep
{
    public VectorStepType Type { get; set; }
    public string? Destination { get; set; }
    public string? QueueName { get; set; }
    public string? Message { get; set; }
    public int Seconds { get; set; }
    public int StepIndex { get; set; }
    public bool BusinessHoursOnly { get; set; }
    public int ElseGotoStep { get; set; }
}

/// <summary>
/// A CM10 call vector — a sequence of routing steps.
/// </summary>
public class CallVector
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<VectorStep> Steps { get; set; } = new();
}

/// <summary>
/// A CM10 ACD queue with agents and routing strategy.
/// </summary>
public class AcdQueue
{
    public string Name { get; set; } = string.Empty;
    public QueueStrategy Strategy { get; set; } = QueueStrategy.RoundRobin;
    public List<string> AgentIds { get; set; } = new();
    public int CallsInQueue { get; set; }
    public int LastAssignedIndex { get; set; }
}

/// <summary>
/// A simulated agent with state and skills.
/// </summary>
public class SimulatedAgent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public AgentState State { get; set; } = AgentState.Available;
    public List<string> Skills { get; set; } = new();
    public string? CurrentCallId { get; set; }
    public int CallsHandled { get; set; }
}

// ════════════════════════════════════════════════════════════
//  Configuration Section Models (for IConfiguration binding)
// ════════════════════════════════════════════════════════════

public class PstnConfig
{
    public List<GatewayConfig> Gateways { get; set; } = new();
    public List<TrunkConfig> Trunks { get; set; } = new();
    public List<SimulatedDid> Dids { get; set; } = new();
    public List<SimulatedCaller> Callers { get; set; } = new();
}

public class Cm10Config
{
    public List<Vdn> Vdns { get; set; } = new();
    public List<CallVector> Vectors { get; set; } = new();
    public List<AcdQueue> Queues { get; set; } = new();
    public List<SimulatedAgent> Agents { get; set; } = new();
}
