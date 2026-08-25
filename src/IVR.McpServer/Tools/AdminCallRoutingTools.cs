using System.ComponentModel;
using IVR.Core.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IVR.McpServer.Tools;

[McpServerToolType]
public class AdminCallRoutingTools
{
    private readonly IAdminCallRoutingService _routingService;
    private readonly ILogger<AdminCallRoutingTools> _logger;

    public AdminCallRoutingTools(IAdminCallRoutingService routingService, ILogger<AdminCallRoutingTools> logger)
    {
        _routingService = routingService;
        _logger = logger;
    }

    [McpServerTool(Name = "route_admin_call")]
    [Description(
        "Deterministically routes an admin call to a team based on call type and business " +
        "hours — no AI transcript classification. Call this once a call type is known (e.g. " +
        "from a menu selection). Escalate=true means route to the fallback/on-call path " +
        "instead of the matched team's direct transfer number.")]
    public async Task<AdminCallRouteToolResult> RouteAdminCallAsync(
        [Description("The call type/intent, matched against team names and intent keywords, e.g. 'fire alarm'.")] string callType,
        [Description("Optional facility identifier for context/logging.")] string? facilityId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callType))
        {
            throw new ArgumentException("callType is required.", nameof(callType));
        }

        var result = await _routingService.DetermineRouteAsync(callType, facilityId, cancellationToken);

        _logger.LogInformation(
            "route_admin_call: callType={CallType} matched={Matched} team={TeamName} escalate={Escalate}",
            callType, result.Matched, result.TeamName, result.Escalate);

        return new AdminCallRouteToolResult(
            result.Matched,
            result.TeamName,
            result.TransferNumber,
            result.QueueName,
            result.IsBusinessHours,
            result.Escalate,
            result.Reason);
    }
}

public record AdminCallRouteToolResult(
    bool Matched,
    string? TeamName,
    string? TransferNumber,
    string? QueueName,
    bool IsBusinessHours,
    bool Escalate,
    string Reason);
