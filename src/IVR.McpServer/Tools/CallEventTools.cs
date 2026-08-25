using System.ComponentModel;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IVR.McpServer.Tools;

[McpServerToolType]
public class CallEventTools
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<CallEventTools> _logger;

    public CallEventTools(ICosmosDbService cosmosDb, ILogger<CallEventTools> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [McpServerTool(Name = "record_call_event")]
    [Description(
        "Idempotently records a discrete call event (e.g. a menu action, an external system " +
        "submission, an escalation) against a call. Calling this again with the same eventId " +
        "for the same callId does not create a duplicate — the original recorded event is " +
        "returned unchanged.")]
    public async Task<RecordCallEventResult> RecordCallEventAsync(
        [Description("The call this event belongs to.")] string callId,
        [Description("Caller-supplied idempotency key for this event. Reusing it is safe and a no-op.")] string eventId,
        [Description("Event type, e.g. 'MenuAction', 'ExternalSystemSubmitted', 'Escalated'.")] string eventType,
        [Description("Event time in UTC (ISO 8601). Defaults to now if omitted.")] DateTime? timestampUtc = null,
        [Description("Structured, non-sensitive details about the event.")] Dictionary<string, string>? details = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callId))
        {
            throw new ArgumentException("callId is required.", nameof(callId));
        }
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("eventId is required.", nameof(eventId));
        }
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("eventType is required.", nameof(eventType));
        }

        var existing = await _cosmosDb.GetCallEventAsync(callId, eventId);
        if (existing is not null)
        {
            _logger.LogInformation("record_call_event: eventId={EventId} already recorded for callId={CallId} — no-op", eventId, callId);
            return new RecordCallEventResult(existing.Id, existing.RecordedAt, AlreadyRecorded: true);
        }

        var callEvent = new CallEvent
        {
            Id = eventId,
            CallId = callId,
            EventType = eventType,
            TimestampUtc = timestampUtc ?? DateTime.UtcNow,
            Details = details ?? new Dictionary<string, string>()
        };

        var stored = await _cosmosDb.RecordCallEventAsync(callEvent);

        _logger.LogInformation("record_call_event: eventId={EventId} recorded for callId={CallId}", eventId, callId);

        return new RecordCallEventResult(stored.Id, stored.RecordedAt, AlreadyRecorded: false);
    }
}

public record RecordCallEventResult(string EventId, DateTime RecordedAt, bool AlreadyRecorded);
