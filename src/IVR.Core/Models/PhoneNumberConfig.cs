using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Represents the type of PSTN connection for a phone number.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PstnNumberType
{
    /// <summary>Phone number purchased through Azure Communication Services.</summary>
    NativeAcs,

    /// <summary>Phone number routed via SIP direct routing (customer-owned SBC).</summary>
    DirectRouting,

    /// <summary>Phone number routed via SIP trunk provider (e.g., Twilio Elastic SIP, Bandwidth).</summary>
    SipTrunk
}

/// <summary>
/// Per-phone-number configuration — maps an inbound DID to its IVR entry point and PSTN connection settings.
/// Enables multi-DID support: each phone number can have its own root menu, business hours, and SBC routing.
/// </summary>
public class PhoneNumberConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The phone number in E.164 format (e.g., "+15551234567").
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the number (e.g., "Main Line", "Sales Hotline").
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// How this number is connected to Azure Communication Services.
    /// </summary>
    [JsonPropertyName("numberType")]
    public PstnNumberType NumberType { get; set; } = PstnNumberType.NativeAcs;

    /// <summary>
    /// IVR root menu to use when a call arrives on this number.
    /// If null, the system-wide root menu from SystemConfig is used.
    /// </summary>
    [JsonPropertyName("rootMenuId")]
    public string? RootMenuId { get; set; }

    /// <summary>
    /// Optional per-number business hours config override.
    /// If null, the system-wide business hours apply.
    /// </summary>
    [JsonPropertyName("businessHoursConfigId")]
    public string? BusinessHoursConfigId { get; set; }

    /// <summary>
    /// Optional welcome prompt that plays before the root menu.
    /// </summary>
    [JsonPropertyName("welcomePromptId")]
    public string? WelcomePromptId { get; set; }

    /// <summary>
    /// FQDN of the Session Border Controller for direct routing / SIP trunk connections.
    /// Required when NumberType is DirectRouting or SipTrunk. Example: "sbc.contoso.com"
    /// </summary>
    [JsonPropertyName("sbcFqdn")]
    public string? SbcFqdn { get; set; }

    /// <summary>
    /// SIP signaling port (default 5067 for TLS).
    /// </summary>
    [JsonPropertyName("sbcPort")]
    public int SbcPort { get; set; } = 5067;

    /// <summary>
    /// Alternative called-number identities that should be treated as this number.
    /// Useful when SBC rewrites or carrier aliases send different caller/called IDs.
    /// </summary>
    [JsonPropertyName("calledNumberAliases")]
    public List<string> CalledNumberAliases { get; set; } = new();

    /// <summary>
    /// Whether this phone number is actively receiving calls.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "phone-number";
}
