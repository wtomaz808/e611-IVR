using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Automatic Number Identification record — identifies the caller.
/// </summary>
public class AniRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("callerName")]
    public string? CallerName { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("callerType")]
    public CallerType CallerType { get; set; } = CallerType.Unknown;

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-US";

    [JsonPropertyName("vipFlag")]
    public bool IsVip { get; set; }

    [JsonPropertyName("blockedFlag")]
    public bool IsBlocked { get; set; }

    [JsonPropertyName("customRouting")]
    public string? CustomRoutingMenuId { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => PhoneNumber[..Math.Min(4, PhoneNumber.Length)];
}

public enum CallerType
{
    Unknown,
    Residential,
    Business,
    Government,
    Emergency,
    Internal
}
