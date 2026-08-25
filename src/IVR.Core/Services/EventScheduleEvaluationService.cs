using IVR.Core.Interfaces;
using IVR.Core.Models;

namespace IVR.Core.Services;

public class EventScheduleEvaluationService : IEventScheduleEvaluationService
{
    private readonly ICosmosDbService _cosmosDb;

    public EventScheduleEvaluationService(ICosmosDbService cosmosDb)
    {
        _cosmosDb = cosmosDb;
    }

    public async Task<EventScheduleEvaluationResult> EvaluateAsync(
        string facilityId,
        string? deviceId,
        DateTime eventTimeUtc,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _cosmosDb.GetActiveEventSchedulesForFacilityAsync(facilityId, deviceId);

        // Prefer a device-specific match over a facility-wide one when both cover the time.
        var match = candidates
            .Where(s => eventTimeUtc >= s.StartTimeUtc && eventTimeUtc <= s.EndTimeUtc)
            .OrderByDescending(s => s.DeviceId != null)
            .FirstOrDefault();

        if (match is null)
        {
            return new EventScheduleEvaluationResult(
                Matched: false,
                ScheduleId: null,
                EventType: null,
                IsApproved: false,
                ApprovingContact: null,
                WindowStartUtc: null,
                WindowEndUtc: null,
                Reason: "No active schedule covers this facility/device at the given time — treat as unscheduled.");
        }

        return new EventScheduleEvaluationResult(
            Matched: true,
            ScheduleId: match.Id,
            EventType: match.EventType,
            IsApproved: match.IsApproved,
            ApprovingContact: match.ApprovingContact,
            WindowStartUtc: match.StartTimeUtc,
            WindowEndUtc: match.EndTimeUtc,
            Reason: match.IsApproved
                ? "Matches an approved schedule window."
                : "Matches a schedule window that is not yet approved — escalate for confirmation.");
    }
}
