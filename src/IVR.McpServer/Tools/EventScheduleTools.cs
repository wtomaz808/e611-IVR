using System.ComponentModel;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IVR.McpServer.Tools;

[McpServerToolType]
public class EventScheduleTools
{
    private readonly IEventScheduleEvaluationService _evaluationService;
    private readonly ILogger<EventScheduleTools> _logger;

    public EventScheduleTools(IEventScheduleEvaluationService evaluationService, ILogger<EventScheduleTools> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    [McpServerTool(Name = "check_event_schedule")]
    [Description(
        "Checks whether a facility/device event (e.g. a fire alarm test) falls inside a known, " +
        "approved schedule window. Call this before treating an admin call as unscheduled. " +
        "Matched=false or IsApproved=false means the event should follow the escalation path.")]
    public async Task<EventScheduleCheckResult> CheckEventScheduleAsync(
        [Description("Facility identifier, e.g. 'building-4'.")] string facilityId,
        [Description("Optional device identifier within the facility, e.g. 'panel-3'.")] string? deviceId = null,
        [Description("Event time in UTC (ISO 8601). Defaults to now if omitted.")] DateTime? eventTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(facilityId))
        {
            throw new ArgumentException("facilityId is required.", nameof(facilityId));
        }

        var result = await _evaluationService.EvaluateAsync(facilityId, deviceId, eventTimeUtc ?? DateTime.UtcNow, cancellationToken);

        _logger.LogInformation(
            "check_event_schedule: facility={FacilityId} device={DeviceId} matched={Matched} approved={IsApproved}",
            facilityId, deviceId, result.Matched, result.IsApproved);

        return new EventScheduleCheckResult(
            result.Matched,
            result.ScheduleId,
            result.EventType?.ToString(),
            result.IsApproved,
            result.ApprovingContact,
            result.WindowStartUtc,
            result.WindowEndUtc,
            result.Reason);
    }
}

public record EventScheduleCheckResult(
    bool Matched,
    string? ScheduleId,
    string? EventType,
    bool IsApproved,
    string? ApprovingContact,
    DateTime? WindowStartUtc,
    DateTime? WindowEndUtc,
    string Reason);
