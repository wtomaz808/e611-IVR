using IVR.Core.Models;

namespace IVR.Core.Interfaces;

/// <summary>
/// Deterministic evaluation of whether an admin call about a facility/device event falls
/// inside a known, approved schedule window (e.g. a monthly fire alarm test), or should be
/// treated as unscheduled and escalated. Backs the "check_event_schedule" MCP tool.
/// </summary>
public interface IEventScheduleEvaluationService
{
    Task<EventScheduleEvaluationResult> EvaluateAsync(
        string facilityId,
        string? deviceId,
        DateTime eventTimeUtc,
        CancellationToken cancellationToken = default);
}

public record EventScheduleEvaluationResult(
    bool Matched,
    string? ScheduleId,
    ScheduledEventType? EventType,
    bool IsApproved,
    string? ApprovingContact,
    DateTime? WindowStartUtc,
    DateTime? WindowEndUtc,
    string Reason);
