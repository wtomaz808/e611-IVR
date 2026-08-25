using IVR.Core.Models;

namespace IVR.Core.Interfaces;

/// <summary>
/// Deterministic (non-AI) routing decision for admin calls: matches a call type against
/// active <see cref="TeamRoutingConfig"/> entries by keyword/name, and factors in business
/// hours. Distinct from <c>TranscriptRoutingService</c> (AI transcript classification) and
/// <c>ICallFlowEngine</c> (DTMF/speech menu-tree navigation) — this backs the
/// "route_admin_call" MCP tool, which takes a call type + facility context rather than a
/// live call's menu state.
/// </summary>
public interface IAdminCallRoutingService
{
    Task<AdminCallRouteResult> DetermineRouteAsync(
        string callType,
        string? facilityId = null,
        CancellationToken cancellationToken = default);
}

public record AdminCallRouteResult(
    bool Matched,
    string? TeamName,
    string? TransferNumber,
    string? QueueName,
    bool IsBusinessHours,
    bool Escalate,
    string Reason);
