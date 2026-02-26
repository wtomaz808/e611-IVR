using System.Collections.Concurrent;
using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Central in-memory store for all active and completed calls.
/// Provides events for real-time UI updates via Blazor Server.
/// </summary>
public class CallStateManager
{
    private readonly ConcurrentDictionary<string, SimulatedCall> _activeCalls = new();
    private readonly ConcurrentQueue<CallDetailRecord> _cdrs = new();
    private readonly ConcurrentDictionary<string, string> _connectionToCallMap = new();

    // ─── Events for UI refresh ──────────────────────────────
    public event Action? OnCallsChanged;
    public event Action<SimulatedCall>? OnCallUpdated;
    public event Action<CallDetailRecord>? OnCallCompleted;

    // ─── Active call management ─────────────────────────────

    public SimulatedCall CreateCall(SimulatedCaller caller, SimulatedDid did)
    {
        var call = new SimulatedCall
        {
            CallerNumber = caller.Number,
            CallerName = caller.Name,
            CallerType = caller.Type,
            DidNumber = did.Number,
            DidLabel = did.Label
        };

        _activeCalls[call.Id] = call;
        NotifyCallsChanged();
        return call;
    }

    public SimulatedCall? GetCall(string callId) =>
        _activeCalls.TryGetValue(callId, out var call) ? call : null;

    public SimulatedCall? GetCallByConnectionId(string connectionId) =>
        _connectionToCallMap.TryGetValue(connectionId, out var callId) ? GetCall(callId) : null;

    public IReadOnlyCollection<SimulatedCall> GetActiveCalls() =>
        _activeCalls.Values.Where(c => c.State != CallState.Disconnected && c.State != CallState.Failed)
            .OrderByDescending(c => c.StartTime).ToList();

    public IReadOnlyCollection<SimulatedCall> GetAllCalls() =>
        _activeCalls.Values.OrderByDescending(c => c.StartTime).ToList();

    // ─── Connection mapping (ACS callConnectionId → sim call) ──

    public void RegisterConnection(string callId, string connectionId, string callbackUri, string serverCallId)
    {
        if (_activeCalls.TryGetValue(callId, out var call))
        {
            call.CallConnectionId = connectionId;
            call.ServerCallId = serverCallId;
            call.CallbackUri = callbackUri;
            _connectionToCallMap[connectionId] = callId;
            NotifyCallUpdated(call);
        }
    }

    // ─── State transitions ──────────────────────────────────

    public void UpdateCall(string callId, Action<SimulatedCall> update)
    {
        if (_activeCalls.TryGetValue(callId, out var call))
        {
            update(call);
            NotifyCallUpdated(call);
        }
    }

    public void TransitionCall(string callId, CallState newState, CallEventSource source, string message, string? detail = null)
    {
        if (_activeCalls.TryGetValue(callId, out var call))
        {
            call.TransitionTo(newState, source, message, detail);
            NotifyCallUpdated(call);
        }
    }

    public void CompleteCall(string callId, CallState finalState, CallEventSource source, string message)
    {
        if (_activeCalls.TryGetValue(callId, out var call))
        {
            call.EndTime = DateTime.UtcNow;
            call.TransitionTo(finalState, source, message);

            // Create CDR
            var cdr = new CallDetailRecord
            {
                CallId = call.Id,
                CallerNumber = call.CallerNumber,
                CallerName = call.CallerName,
                DidNumber = call.DidNumber,
                VdnNumber = call.VdnNumber,
                TrunkName = call.TrunkName,
                QueueName = call.QueueName,
                AgentName = call.AssignedAgentName,
                StartTime = call.StartTime,
                EndTime = call.EndTime,
                DurationSeconds = call.DurationSeconds,
                FinalState = finalState,
                EventCount = call.Events.Count,
                DtmfInputs = new List<string>(call.DtmfInputs)
            };

            _cdrs.Enqueue(cdr);

            // Remove connection mapping
            if (call.CallConnectionId != null)
                _connectionToCallMap.TryRemove(call.CallConnectionId, out _);

            NotifyCallUpdated(call);
            OnCallCompleted?.Invoke(cdr);
        }
    }

    // ─── CDR history ────────────────────────────────────────

    public IReadOnlyCollection<CallDetailRecord> GetRecentCdrs(int count = 50) =>
        _cdrs.Reverse().Take(count).ToList();

    // ─── Notifications ──────────────────────────────────────

    private void NotifyCallsChanged() => OnCallsChanged?.Invoke();
    private void NotifyCallUpdated(SimulatedCall call)
    {
        OnCallUpdated?.Invoke(call);
        OnCallsChanged?.Invoke();
    }
}
