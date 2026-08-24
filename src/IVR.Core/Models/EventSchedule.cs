using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// A known/approved maintenance or test window for a facility device (e.g. a monthly
/// fire alarm test). Used by the "check_event_schedule" MCP tool to distinguish routine,
/// pre-approved admin calls from unscheduled events that should escalate.
/// </summary>
public class EventSchedule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Facility identifier this schedule applies to (e.g. "building-4").
    /// </summary>
    [JsonPropertyName("facilityId")]
    public string FacilityId { get; set; } = string.Empty;

    /// <summary>
    /// Optional specific device within the facility (e.g. "panel-3").
    /// Null means the schedule applies to the whole facility.
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("eventType")]
    public ScheduledEventType EventType { get; set; } = ScheduledEventType.ScheduledTest;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Window start, in UTC.
    /// </summary>
    [JsonPropertyName("startTimeUtc")]
    public DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// Window end, in UTC.
    /// </summary>
    [JsonPropertyName("endTimeUtc")]
    public DateTime EndTimeUtc { get; set; }

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "America/New_York";

    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }

    /// <summary>
    /// Contact who approved this window (name or email), surfaced in confirmations/logs.
    /// </summary>
    [JsonPropertyName("approvingContact")]
    public string? ApprovingContact { get; set; }

    [JsonPropertyName("isRecurring")]
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Freeform recurrence description (e.g. "Monthly, first Tuesday"). Not evaluated
    /// programmatically yet — each occurrence is still seeded as its own record.
    /// </summary>
    [JsonPropertyName("recurrenceRule")]
    public string? RecurrenceRule { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "event-schedule";
}

public enum ScheduledEventType
{
    ScheduledTest,
    Maintenance,
    Inspection,
    Other
}
