using IVR.Core.Interfaces;

namespace IVR.Core.Services;

public class AdminCallRoutingService : IAdminCallRoutingService
{
    private readonly ICosmosDbService _cosmosDb;

    public AdminCallRoutingService(ICosmosDbService cosmosDb)
    {
        _cosmosDb = cosmosDb;
    }

    public async Task<AdminCallRouteResult> DetermineRouteAsync(
        string callType,
        string? facilityId = null,
        CancellationToken cancellationToken = default)
    {
        var businessHoursTask = _cosmosDb.GetBusinessHoursAsync();
        var teamsTask = _cosmosDb.GetAllActiveTeamRoutingConfigsAsync();
        await Task.WhenAll(businessHoursTask, teamsTask);

        var isBusinessHours = businessHoursTask.Result?.IsCurrentlyOpen() ?? true;

        var match = teamsTask.Result
            .Where(t => t.TeamName.Equals(callType, StringComparison.OrdinalIgnoreCase)
                        || t.IntentKeywords.Any(k => k.Equals(callType, StringComparison.OrdinalIgnoreCase)
                                                     || callType.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefault();

        if (match is null)
        {
            return new AdminCallRouteResult(
                Matched: false,
                TeamName: null,
                TransferNumber: null,
                QueueName: null,
                IsBusinessHours: isBusinessHours,
                Escalate: true,
                Reason: $"No active team routing config matches call type '{callType}' — escalate.");
        }

        return new AdminCallRouteResult(
            Matched: true,
            TeamName: match.TeamName,
            TransferNumber: match.TransferNumber,
            QueueName: match.QueueName,
            IsBusinessHours: isBusinessHours,
            Escalate: !isBusinessHours,
            Reason: isBusinessHours
                ? $"Matched team '{match.TeamName}'."
                : $"Matched team '{match.TeamName}' but outside business hours — escalate.");
    }
}
