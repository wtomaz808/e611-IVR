using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Simulates an Avaya CM10 — VDN lookup, call vector evaluation, ACD queue routing.
/// </summary>
public class Cm10Service
{
    private readonly List<Vdn> _vdns;
    private readonly List<CallVector> _vectors;
    private readonly List<AcdQueue> _queues;
    private readonly List<SimulatedAgent> _agents;
    private readonly ILogger<Cm10Service> _logger;
    private readonly object _lock = new();

    public Cm10Service(IConfiguration config, ILogger<Cm10Service> logger)
    {
        _logger = logger;

        var cm10Config = new Cm10Config();
        config.GetSection("Cm10").Bind(cm10Config);

        _vdns = cm10Config.Vdns;
        _vectors = cm10Config.Vectors;
        _queues = cm10Config.Queues;
        _agents = cm10Config.Agents;
    }

    public IReadOnlyList<Vdn> Vdns => _vdns;
    public IReadOnlyList<CallVector> Vectors => _vectors;
    public IReadOnlyList<AcdQueue> Queues => _queues;
    public IReadOnlyList<SimulatedAgent> Agents => _agents;

    public IReadOnlyList<Vdn> GetVdns() => _vdns;
    public IReadOnlyList<CallVector> GetVectors() => _vectors;
    public IReadOnlyList<AcdQueue> GetQueues() => _queues;
    public IReadOnlyList<SimulatedAgent> GetAgents() => _agents;

    /// <summary>
    /// Look up a VDN by number.
    /// </summary>
    public Vdn? GetVdn(string vdnNumber)
    {
        return _vdns.FirstOrDefault(v => v.Number == vdnNumber);
    }

    /// <summary>
    /// Evaluate a VDN and determine routing — returns the destination.
    /// Destination can be: "IVR", "Queue:queue-name", "VDN:number", "Announcement:message"
    /// </summary>
    public VdnRoutingResult RouteVdn(string vdnNumber)
    {
        var vdn = GetVdn(vdnNumber);
        if (vdn == null)
        {
            _logger.LogWarning("VDN {Vdn} not found", vdnNumber);
            return new VdnRoutingResult { Success = false, Error = $"VDN {vdnNumber} not found" };
        }

        var vector = _vectors.FirstOrDefault(v => v.Id == vdn.VectorId);
        if (vector == null)
        {
            // No vector — use the VDN's direct destination
            return new VdnRoutingResult
            {
                Success = true,
                VdnNumber = vdn.Number,
                VdnLabel = vdn.Label,
                Destination = vdn.Destination,
                VectorName = "(direct)"
            };
        }

        _logger.LogInformation("Evaluating vector {Vector} for VDN {Vdn}", vector.Name, vdnNumber);

        // Execute vector steps
        for (int i = 0; i < vector.Steps.Count; i++)
        {
            var step = vector.Steps[i];

            switch (step.Type)
            {
                case VectorStepType.Route:
                    return new VdnRoutingResult
                    {
                        Success = true,
                        VdnNumber = vdn.Number,
                        VdnLabel = vdn.Label,
                        Destination = step.Destination ?? vdn.Destination,
                        VectorName = vector.Name,
                        VectorStepsExecuted = i + 1
                    };

                case VectorStepType.Queue:
                    return new VdnRoutingResult
                    {
                        Success = true,
                        VdnNumber = vdn.Number,
                        VdnLabel = vdn.Label,
                        Destination = $"Queue:{step.QueueName}",
                        VectorName = vector.Name,
                        VectorStepsExecuted = i + 1
                    };

                case VectorStepType.TimeCheck:
                    // Simplified: always assume business hours for now
                    _logger.LogInformation("Vector step {Step}: TimeCheck — assuming business hours", i);
                    break; // Continue to next step

                case VectorStepType.Announcement:
                    _logger.LogInformation("Vector step {Step}: Announcement — {Message}", i, step.Message);
                    // Announcements are informational, continue to next step
                    break;

                case VectorStepType.Disconnect:
                    return new VdnRoutingResult
                    {
                        Success = true,
                        VdnNumber = vdn.Number,
                        VdnLabel = vdn.Label,
                        Destination = "Disconnect",
                        VectorName = vector.Name,
                        VectorStepsExecuted = i + 1
                    };

                case VectorStepType.Goto:
                    i = step.StepIndex - 1; // -1 because loop will increment
                    break;

                case VectorStepType.Wait:
                    // In a real sim we'd delay; here we just log and continue
                    _logger.LogInformation("Vector step {Step}: Wait {Sec}s", i, step.Seconds);
                    break;
            }
        }

        // Fell through vector — use VDN's default destination
        return new VdnRoutingResult
        {
            Success = true,
            VdnNumber = vdn.Number,
            VdnLabel = vdn.Label,
            Destination = vdn.Destination,
            VectorName = vector.Name,
            VectorStepsExecuted = vector.Steps.Count
        };
    }

    /// <summary>
    /// Assign a call from an ACD queue to an available agent.
    /// </summary>
    public AgentAssignment? AssignAgent(string queueName)
    {
        lock (_lock)
        {
            var queue = _queues.FirstOrDefault(q => q.Name == queueName);
            if (queue == null)
            {
                _logger.LogWarning("Queue {Queue} not found", queueName);
                return null;
            }

            // Find available agents in this queue
            var availableAgents = _agents
                .Where(a => queue.AgentIds.Contains(a.Id) && a.State == AgentState.Available)
                .ToList();

            if (availableAgents.Count == 0)
            {
                _logger.LogInformation("No available agents in queue {Queue}", queueName);
                queue.CallsInQueue++;
                return null;
            }

            SimulatedAgent? selected = null;

            switch (queue.Strategy)
            {
                case QueueStrategy.RoundRobin:
                    var idx = queue.LastAssignedIndex % availableAgents.Count;
                    selected = availableAgents[idx];
                    queue.LastAssignedIndex++;
                    break;

                case QueueStrategy.MostIdle:
                    selected = availableAgents.OrderBy(a => a.CallsHandled).First();
                    break;

                case QueueStrategy.Linear:
                    selected = availableAgents.First();
                    break;
            }

            if (selected != null)
            {
                selected.State = AgentState.Ringing;
                _logger.LogInformation("Assigned agent {Agent} ({Ext}) from queue {Queue}",
                    selected.Name, selected.Extension, queueName);

                return new AgentAssignment
                {
                    AgentId = selected.Id,
                    AgentName = selected.Name,
                    Extension = selected.Extension,
                    QueueName = queueName
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Mark an agent as on-call (they answered).
    /// </summary>
    public void AgentAnswered(string agentId, string callId)
    {
        lock (_lock)
        {
            var agent = _agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null)
            {
                agent.State = AgentState.OnCall;
                agent.CurrentCallId = callId;
                agent.CallsHandled++;
            }
        }
    }

    /// <summary>
    /// Release an agent back to available (call ended).
    /// </summary>
    public void AgentReleased(string agentId)
    {
        lock (_lock)
        {
            var agent = _agents.FirstOrDefault(a => a.Id == agentId);
            if (agent != null)
            {
                agent.State = AgentState.Available;
                agent.CurrentCallId = null;
            }
        }
    }

    /// <summary>
    /// Handle a transfer back to a CM10 VDN from the IVR.
    /// Also handles direct queue transfers (sip:queue-{name}@sbc).
    /// </summary>
    public VdnRoutingResult HandleTransferToVdn(string targetAddress)
    {
        // Extract VDN number from SIP URI, extension, or E.164
        string vdnNumber;

        if (targetAddress.StartsWith("sip:", StringComparison.OrdinalIgnoreCase))
        {
            // sip:70100@sbc.contoso.com → 70100
            // sip:queue-inspection-scheduling@sbc.contoso.com → queue-inspection-scheduling
            var parts = targetAddress[4..].Split('@');
            vdnNumber = parts[0].TrimStart('+');

            // Direct queue transfer — bypass VDN lookup
            if (vdnNumber.StartsWith("queue-", StringComparison.OrdinalIgnoreCase))
            {
                var queueName = vdnNumber[6..]; // strip "queue-" prefix
                _logger.LogInformation("Direct queue transfer: {Target} → Queue:{Queue}", targetAddress, queueName);
                return new VdnRoutingResult
                {
                    Success = true,
                    VdnNumber = vdnNumber,
                    VdnLabel = $"Queue: {queueName}",
                    Destination = $"Queue:{queueName}",
                    VectorName = "(direct-queue)"
                };
            }
        }
        else if (targetAddress.StartsWith("+"))
        {
            // E.164 — try last 5 digits as VDN
            vdnNumber = targetAddress.Length > 5 ? targetAddress[^5..] : targetAddress.TrimStart('+');
        }
        else
        {
            vdnNumber = targetAddress;
        }

        _logger.LogInformation("Transfer to VDN: {Target} → resolved VDN {Vdn}", targetAddress, vdnNumber);
        return RouteVdn(vdnNumber);
    }
}

public class VdnRoutingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? VdnNumber { get; set; }
    public string? VdnLabel { get; set; }
    public string? Destination { get; set; }
    public string? VectorName { get; set; }
    public int VectorStepsExecuted { get; set; }
}

public class AgentAssignment
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
