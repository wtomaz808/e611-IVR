using System.Text.Json;
using Azure.Core;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace IVR.Functions.Services.Mcp;

/// <summary>
/// Calls the remote MCP server for each tool when enabled, falling back to the equivalent
/// direct service/domain-service call (the same code paths the MCP server's own tools use
/// under the hood) on failure, timeout, or when MCP is disabled. Never lets an MCP problem
/// break the live call — every method degrades to the direct path unless
/// <see cref="McpGatewayOptions.FallbackToDirect"/> is explicitly turned off.
/// </summary>
public class McpGateway : IMcpGateway, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly McpGatewayOptions _options;
    private readonly TokenCredential _credential;
    private readonly AniAliService _aniAliService;
    private readonly ICosmosDbService _cosmosDb;
    private readonly IEventScheduleEvaluationService _eventScheduleEvaluation;
    private readonly IAdminCallRoutingService _adminCallRouting;
    private readonly ILogger<McpGateway> _logger;

    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private McpClient? _cachedClient;
    private DateTimeOffset _cachedTokenExpiresOn;

    public McpGateway(
        McpGatewayOptions options,
        TokenCredential credential,
        AniAliService aniAliService,
        ICosmosDbService cosmosDb,
        IEventScheduleEvaluationService eventScheduleEvaluation,
        IAdminCallRoutingService adminCallRouting,
        ILogger<McpGateway> logger)
    {
        _options = options;
        _credential = credential;
        _aniAliService = aniAliService;
        _cosmosDb = cosmosDb;
        _eventScheduleEvaluation = eventScheduleEvaluation;
        _adminCallRouting = adminCallRouting;
        _logger = logger;
    }

    // ─── facility_record_lookup ──────────────────────────────────────

    public async Task<FacilityLookupResult> FacilityRecordLookupAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var result = await TryMcpAsync<FacilityLookupResult>(
            "facility_record_lookup",
            new Dictionary<string, object?> { ["phoneNumber"] = phoneNumber },
            cancellationToken);
        if (result is not null) return result;

        // Direct fallback — mirrors IVR.McpServer.Tools.FacilityTools.
        var (ani, ali) = await _aniAliService.LookupCallerAsync(phoneNumber);
        if (ani is null && ali is null)
        {
            return new FacilityLookupResult(false, null, null, false, null, null, null);
        }

        return new FacilityLookupResult(
            Found: true,
            CallerName: ani?.CallerName,
            CallerType: ani?.CallerType.ToString(),
            IsBlocked: ani?.IsBlocked ?? false,
            Facility: ali?.ServiceArea,
            Address: ali is null ? null : $"{ali.Address.City}, {ali.Address.State}".Trim(' ', ','),
            Notes: ani is not null && ani.Metadata.TryGetValue("notes", out var notes) ? notes : null);
    }

    // ─── check_event_schedule ────────────────────────────────────────

    public async Task<EventScheduleCheckResult> CheckEventScheduleAsync(string facilityId, string? deviceId = null, DateTime? eventTimeUtc = null, CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["facilityId"] = facilityId,
            ["deviceId"] = deviceId,
            ["eventTimeUtc"] = eventTimeUtc
        };

        var result = await TryMcpAsync<EventScheduleCheckResult>("check_event_schedule", arguments, cancellationToken);
        if (result is not null) return result;

        // Direct fallback — same IVR.Core domain service the MCP tool wraps, invoked in-process.
        var evaluation = await _eventScheduleEvaluation.EvaluateAsync(facilityId, deviceId, eventTimeUtc ?? DateTime.UtcNow, cancellationToken);
        return new EventScheduleCheckResult(
            evaluation.Matched,
            evaluation.ScheduleId,
            evaluation.EventType?.ToString(),
            evaluation.IsApproved,
            evaluation.ApprovingContact,
            evaluation.WindowStartUtc,
            evaluation.WindowEndUtc,
            evaluation.Reason);
    }

    // ─── get_caller_history ──────────────────────────────────────────

    public async Task<CallerHistoryResult> GetCallerHistoryAsync(string phoneNumber, int limit = 20, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["phoneNumber"] = phoneNumber,
            ["limit"] = limit,
            ["from"] = from,
            ["to"] = to
        };

        var result = await TryMcpAsync<CallerHistoryResult>("get_caller_history", arguments, cancellationToken);
        if (result is not null) return result;

        // Direct fallback — mirrors IVR.McpServer.Tools.CallHistoryTools (transcript excluded).
        var logs = await _cosmosDb.GetCallLogsByPhoneNumberAsync(phoneNumber, Math.Clamp(limit, 1, 50), from, to);
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

    // ─── record_call_event ───────────────────────────────────────────

    public async Task<RecordCallEventResult> RecordCallEventAsync(string callId, string eventId, string eventType, DateTime? timestampUtc = null, Dictionary<string, string>? details = null, CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["callId"] = callId,
            ["eventId"] = eventId,
            ["eventType"] = eventType,
            ["timestampUtc"] = timestampUtc,
            ["details"] = details
        };

        var result = await TryMcpAsync<RecordCallEventResult>("record_call_event", arguments, cancellationToken);
        if (result is not null) return result;

        // Direct fallback — mirrors IVR.McpServer.Tools.CallEventTools (idempotent by eventId).
        var existing = await _cosmosDb.GetCallEventAsync(callId, eventId);
        if (existing is not null)
        {
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
        return new RecordCallEventResult(stored.Id, stored.RecordedAt, AlreadyRecorded: false);
    }

    // ─── route_admin_call ────────────────────────────────────────────

    public async Task<AdminCallRouteResult> RouteAdminCallAsync(string callType, string? facilityId = null, CancellationToken cancellationToken = default)
    {
        var arguments = new Dictionary<string, object?> { ["callType"] = callType, ["facilityId"] = facilityId };

        var result = await TryMcpAsync<AdminCallRouteResult>("route_admin_call", arguments, cancellationToken);
        if (result is not null) return result;

        // Direct fallback — same IVR.Core domain service the MCP tool wraps, invoked in-process.
        var route = await _adminCallRouting.DetermineRouteAsync(callType, facilityId, cancellationToken);
        return new AdminCallRouteResult(
            route.Matched, route.TeamName, route.TransferNumber, route.QueueName, route.IsBusinessHours, route.Escalate, route.Reason);
    }

    // ─── MCP call plumbing ────────────────────────────────────────────

    /// <summary>
    /// Attempts the MCP call and returns the deserialized result, or null to signal the
    /// caller should use its direct fallback. Never throws unless FallbackToDirect is false.
    /// </summary>
    private async Task<T?> TryMcpAsync<T>(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken) where T : class
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

            var client = await GetClientAsync(timeoutCts.Token);
            var result = await client.CallToolAsync(toolName, arguments, cancellationToken: timeoutCts.Token);

            if (result.IsError == true)
            {
                var errorText = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "unknown tool error";
                throw new InvalidOperationException($"MCP tool '{toolName}' returned an error: {errorText}");
            }

            if (result.StructuredContent is { } structured)
            {
                return JsonSerializer.Deserialize<T>(structured.GetRawText(), JsonOptions);
            }

            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
            return text is null ? null : JsonSerializer.Deserialize<T>(text, JsonOptions);
        }
        catch (Exception ex) when (!_options.FallbackToDirect)
        {
            throw new InvalidOperationException($"MCP tool '{toolName}' failed and FallbackToDirect is disabled.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP tool '{ToolName}' failed — falling back to direct call", toolName);
            return null;
        }
    }

    private async Task<McpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_cachedClient is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresOn - TimeSpan.FromMinutes(2))
        {
            return _cachedClient;
        }

        await _clientLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedClient is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresOn - TimeSpan.FromMinutes(2))
            {
                return _cachedClient;
            }

            var tokenContext = new TokenRequestContext([$"{_options.Audience}/.default"]);
            var token = await _credential.GetTokenAsync(tokenContext, cancellationToken);

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_options.Endpoint.TrimEnd('/')}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {token.Token}"
                }
            });

            if (_cachedClient is not null)
            {
                await _cachedClient.DisposeAsync();
            }

            _cachedClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            _cachedTokenExpiresOn = token.ExpiresOn;
            return _cachedClient;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cachedClient is not null)
        {
            await _cachedClient.DisposeAsync();
        }
    }
}
