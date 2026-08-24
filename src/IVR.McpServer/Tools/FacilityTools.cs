using System.ComponentModel;
using IVR.Core.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IVR.McpServer.Tools;

[McpServerToolType]
public class FacilityTools
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<FacilityTools> _logger;

    public FacilityTools(ICosmosDbService cosmosDb, ILogger<FacilityTools> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [McpServerTool(Name = "facility_record_lookup")]
    [Description(
        "Looks up facility identity and location context for an admin call by caller phone number. " +
        "Call this once per incoming call, before routing, to pre-populate facility, device, and " +
        "contact context. Returns Found=false (not an error) when no record matches the number.")]
    public async Task<FacilityLookupResult> FacilityRecordLookupAsync(
        [Description("Caller phone number. Accepts E.164, digits-only, or a SIP/tel URI.")] string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("phoneNumber is required.", nameof(phoneNumber));
        }

        var normalized = NormalizePhoneNumber(phoneNumber);

        var aniTask = _cosmosDb.GetAniRecordAsync(normalized);
        var aliTask = _cosmosDb.GetAliRecordAsync(normalized);
        await Task.WhenAll(aniTask, aliTask);

        var ani = aniTask.Result;
        var ali = aliTask.Result;

        if (ani is null && ali is null)
        {
            _logger.LogInformation("facility_record_lookup: no record for {PhoneNumber}", normalized);
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

    /// <summary>
    /// Normalize to E.164. Mirrors IVR.Functions.Services.AniAliService.NormalizePhoneNumber
    /// so lookups made through the MCP tool and the Function App's direct fallback path
    /// resolve to the same Cosmos DB record.
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.Length == 10)
            return $"+1{digits}";

        if (digits.Length == 11 && digits.StartsWith('1'))
            return $"+{digits}";

        return $"+{digits}";
    }
}

public record FacilityLookupResult(
    bool Found,
    string? CallerName,
    string? CallerType,
    bool IsBlocked,
    string? Facility,
    string? Address,
    string? Notes);
