namespace IVR.Functions.Services.Mcp;

/// <summary>
/// Typed client for the IVR MCP server's tools, with automatic fallback to the equivalent
/// direct service call when MCP is disabled, unreachable, or times out. Mirrors the five
/// tools in <c>IVR.McpServer</c> — see docs/MCP_workflow.md.
/// </summary>
public interface IMcpGateway
{
    Task<FacilityLookupResult> FacilityRecordLookupAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<EventScheduleCheckResult> CheckEventScheduleAsync(string facilityId, string? deviceId = null, DateTime? eventTimeUtc = null, CancellationToken cancellationToken = default);

    Task<CallerHistoryResult> GetCallerHistoryAsync(string phoneNumber, int limit = 20, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    Task<RecordCallEventResult> RecordCallEventAsync(string callId, string eventId, string eventType, DateTime? timestampUtc = null, Dictionary<string, string>? details = null, CancellationToken cancellationToken = default);

    Task<AdminCallRouteResult> RouteAdminCallAsync(string callType, string? facilityId = null, CancellationToken cancellationToken = default);
}

public record FacilityLookupResult(
    bool Found,
    string? CallerName,
    string? CallerType,
    bool IsBlocked,
    string? Facility,
    string? Address,
    string? Notes);

public record EventScheduleCheckResult(
    bool Matched,
    string? ScheduleId,
    string? EventType,
    bool IsApproved,
    string? ApprovingContact,
    DateTime? WindowStartUtc,
    DateTime? WindowEndUtc,
    string Reason);

public record CallSummary(
    string CallId,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    string Disposition,
    string? RoutedToTeam,
    string? TransferredTo);

public record CallerHistoryResult(int Count, List<CallSummary> Calls);

public record RecordCallEventResult(string EventId, DateTime RecordedAt, bool AlreadyRecorded);

public record AdminCallRouteResult(
    bool Matched,
    string? TeamName,
    string? TransferNumber,
    string? QueueName,
    bool IsBusinessHours,
    bool Escalate,
    string Reason);
