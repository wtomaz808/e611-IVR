using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;

namespace IVR.Functions.Services;

/// <summary>
/// ANI/ALI lookup service — resolves caller identity and location data.
/// </summary>
public class AniAliService
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<AniAliService> _logger;

    public AniAliService(ICosmosDbService cosmosDb, ILogger<AniAliService> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    /// <summary>
    /// Performs a combined ANI + ALI lookup for the caller.
    /// </summary>
    public async Task<(AniRecord? Ani, AliRecord? Ali)> LookupCallerAsync(string phoneNumber)
    {
        var normalizedNumber = NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation("Looking up ANI/ALI for {PhoneNumber}", normalizedNumber);

        // Run both lookups in parallel
        var aniTask = _cosmosDb.GetAniRecordAsync(normalizedNumber);
        var aliTask = _cosmosDb.GetAliRecordAsync(normalizedNumber);

        await Task.WhenAll(aniTask, aliTask);

        var ani = aniTask.Result;
        var ali = aliTask.Result;

        if (ani != null)
            _logger.LogInformation("ANI found: {CallerName} ({CallerType})", ani.CallerName, ani.CallerType);
        else
            _logger.LogInformation("No ANI record found for {PhoneNumber}", normalizedNumber);

        if (ali != null)
            _logger.LogInformation("ALI found: {City}, {State}", ali.Address.City, ali.Address.State);
        else
            _logger.LogInformation("No ALI record found for {PhoneNumber}", normalizedNumber);

        return (ani, ali);
    }

    /// <summary>
    /// Check if the caller is blocked based on ANI data.
    /// </summary>
    public async Task<bool> IsCallerBlockedAsync(string phoneNumber)
    {
        var normalizedNumber = NormalizePhoneNumber(phoneNumber);
        var ani = await _cosmosDb.GetAniRecordAsync(normalizedNumber);
        return ani?.IsBlocked ?? false;
    }

    /// <summary>
    /// Determine the caller's preferred language.
    /// </summary>
    public async Task<string> GetCallerLanguageAsync(string phoneNumber, string defaultLanguage = "en-US")
    {
        var normalizedNumber = NormalizePhoneNumber(phoneNumber);
        var ani = await _cosmosDb.GetAniRecordAsync(normalizedNumber);
        return ani?.Language ?? defaultLanguage;
    }

    /// <summary>
    /// Determine the caller's service area from ALI data.
    /// </summary>
    public async Task<string?> GetServiceAreaAsync(string phoneNumber)
    {
        var normalizedNumber = NormalizePhoneNumber(phoneNumber);
        var ali = await _cosmosDb.GetAliRecordAsync(normalizedNumber);
        return ali?.ServiceArea;
    }

    /// <summary>
    /// Extract the phone number from a SIP URI, tel URI, or raw identifier.
    /// Examples:
    ///   sip:+15551234567@sbc.contoso.com      → +15551234567
    ///   sip:15551234567@sbc.contoso.com        → +15551234567
    ///   tel:+15551234567                       → +15551234567
    ///   4:+15551234567                         → +15551234567  (ACS rawId format)
    ///   +15551234567                           → +15551234567
    /// </summary>
    public static string ExtractPhoneFromUri(string rawIdentifier)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
            return "unknown";

        var value = rawIdentifier.Trim();

        // sip:+15551234567@domain.com
        if (value.StartsWith("sip:", StringComparison.OrdinalIgnoreCase))
        {
            value = value[4..];               // strip the sip: prefix
            var atIndex = value.IndexOf('@');
            if (atIndex > 0)
                value = value[..atIndex];      // strip @domain
        }
        // tel:+15551234567
        else if (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            value = value[4..];
        }
        // ACS rawId format — "4:+15551234567"
        else if (value.Contains(':'))
        {
            value = value[(value.LastIndexOf(':') + 1)..];
        }

        return NormalizePhoneNumber(value);
    }

    /// <summary>
    /// Normalize phone number to E.164 format.
    /// </summary>
    public static string NormalizePhoneNumber(string phoneNumber)
    {
        // Strip all non-numeric characters except leading +
        var hasPlus = phoneNumber.StartsWith('+');
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // If 10 digits, assume US number and prepend +1
        if (digits.Length == 10)
            return $"+1{digits}";

        // If 11 digits starting with 1, add +
        if (digits.Length == 11 && digits.StartsWith('1'))
            return $"+{digits}";

        // If already has +, return as-is
        if (hasPlus)
            return $"+{digits}";

        return $"+{digits}";
    }
}
