using System.ComponentModel;
using IVR.Core.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IVR.McpServer.Tools;

[McpServerToolType]
public class CallHistoryTools
{
    private const int MaxLimit = 50;

    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<CallHistoryTools> _logger;

    public CallHistoryTools(ICosmosDbService cosmosDb, ILogger<CallHistoryTools> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [McpServerTool(Name = "get_caller_history")]
    [Description(
        "Returns a bounded summary of prior calls from a phone number (disposition, routing, " +
        "timestamps). Call transcripts are never included — use this for call-pattern context, " +
        "not transcript review.")]
    public async Task<CallerHistoryResult> GetCallerHistoryAsync(
        [Description("Caller phone number. Accepts E.164, digits-only, or a SIP/tel URI.")] string phoneNumber,
        [Description("Maximum number of calls to return (default 20, capped at 50).")] int limit = 20,
        [Description("Optional start of the date range (UTC). Defaults to the last 90 days.")] DateTime? from = null,
        [Description("Optional end of the date range (UTC). Defaults to now.")] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("phoneNumber is required.", nameof(phoneNumber));
        }

        var boundedLimit = Math.Clamp(limit, 1, MaxLimit);
        var logs = await _cosmosDb.GetCallLogsByPhoneNumberAsync(phoneNumber, boundedLimit, from, to);

        _logger.LogInformation("get_caller_history: phoneNumber={PhoneNumber} returned={Count}", phoneNumber, logs.Count);

        var summaries = logs.Select(log => new CallSummary(
            log.CallId,
            log.StartTime,
            log.EndTime,
            log.Status.ToString(),
            log.Disposition.ToString(),
            log.RoutedToTeam,
            log.TransferredTo)).ToList();

        return new CallerHistoryResult(summaries.Count, summaries);
    }
}

public record CallSummary(
    string CallId,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    string Disposition,
    string? RoutedToTeam,
    string? TransferredTo);

public record CallerHistoryResult(int Count, List<CallSummary> Calls);
