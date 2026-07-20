using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Describes how the IVR system connects to the PSTN.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PstnMode
{
    /// <summary>Phone numbers are purchased inside Azure Communication Services.</summary>
    NativeAcs,

    /// <summary>All numbers arrive via customer-owned SBC / SIP trunk (direct routing).</summary>
    DirectRouting,

    /// <summary>Mix of ACS-native and direct-routed numbers.</summary>
    Hybrid,

    /// <summary>
    /// PSTN calls arrive via Microsoft Teams Phone System (Calling Plans or Operator Connect).
    /// The IVR engine is driven by a Teams Calling Bot using the Microsoft Graph Calling API.
    /// No customer-managed SBC or Azure Communication Services required.
    /// </summary>
    TeamsBot
}

/// <summary>
/// System-wide IVR configuration settings.
/// </summary>
public class SystemConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "system-config";

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "en-US";

    [JsonPropertyName("defaultTtsVoice")]
    public string DefaultTtsVoice { get; set; } = "en-US-JennyNeural";

    [JsonPropertyName("maxCallDurationMinutes")]
    public int MaxCallDurationMinutes { get; set; } = 60;

    [JsonPropertyName("recordCalls")]
    public bool RecordCalls { get; set; } = false;

    [JsonPropertyName("enableSpeechRecognition")]
    public bool EnableSpeechRecognition { get; set; } = true;

    [JsonPropertyName("rootMenuId")]
    public string? RootMenuId { get; set; }

    [JsonPropertyName("emergencyMenuId")]
    public string? EmergencyMenuId { get; set; }

    [JsonPropertyName("globalTimeoutSeconds")]
    public int GlobalTimeoutSeconds { get; set; } = 15;

    [JsonPropertyName("globalMaxRetries")]
    public int GlobalMaxRetries { get; set; } = 3;

    /// <summary>
    /// How the system connects to the PSTN — NativeAcs, DirectRouting, or Hybrid.
    /// </summary>
    [JsonPropertyName("pstnMode")]
    public PstnMode PstnMode { get; set; } = PstnMode.NativeAcs;

    /// <summary>
    /// Default Session Border Controller FQDN used when PstnMode is DirectRouting.
    /// Individual phone numbers can override this in PhoneNumberConfig.
    /// </summary>
    [JsonPropertyName("defaultSbcFqdn")]
    public string? DefaultSbcFqdn { get; set; }

    /// <summary>
    /// Default SBC SIP signaling port (typically 5067 for TLS).
    /// </summary>
    [JsonPropertyName("defaultSbcPort")]
    public int DefaultSbcPort { get; set; } = 5067;

    // ─── Avaya CM10 Integration ──────────────────────────────────

    /// <summary>
    /// When true, caller disconnects are forwarded (transferred) to the configured
    /// CM10 VDN instead of simply ending the call.  This allows the Avaya ACD
    /// to handle post-IVR routing (e.g. agent queuing, wrap-up).
    /// </summary>
    [JsonPropertyName("enableDisconnectTransferToVdn")]
    public bool EnableDisconnectTransferToVdn { get; set; }

    /// <summary>
    /// The SIP URI or E.164 number of the default Avaya CM10 VDN that receives
    /// calls when the IVR call flow completes or the caller hangs up.
    /// Example: "sip:70100@sbc.contoso.com" or "+18005550100".
    /// </summary>
    [JsonPropertyName("defaultCm10Vdn")]
    public string? DefaultCm10Vdn { get; set; }

    /// <summary>
    /// FQDN of the Avaya Session Border Controller used for SIP-based
    /// VDN transfers. Falls back to DefaultSbcFqdn if not set.
    /// </summary>
    [JsonPropertyName("cm10SbcFqdn")]
    public string? Cm10SbcFqdn { get; set; }

    /// <summary>
    /// SIP port on the Avaya SBC (defaults to DefaultSbcPort).
    /// </summary>
    [JsonPropertyName("cm10SbcPort")]
    public int? Cm10SbcPort { get; set; }

    /// <summary>
    /// Optional prompt to play to the caller before transferring to the CM10 VDN
    /// (e.g. "Please hold while we connect you.").
    /// </summary>
    [JsonPropertyName("cm10TransferPromptId")]
    public string? Cm10TransferPromptId { get; set; }

    // ─── Teams Integration ──────────────────────────────

    /// <summary>
    /// The Entra App Registration Application (client) ID used by the Teams Calling Bot.
    /// Non-sensitive — this is the public identifier, not the secret.
    /// Displayed in the admin portal for operator reference.
    /// </summary>
    [JsonPropertyName("teamsBotAppId")]
    public string? TeamsBotAppId { get; set; }

    /// <summary>
    /// The Entra tenant ID that owns the bot App Registration.
    /// </summary>
    [JsonPropertyName("teamsTenantId")]
    public string? TeamsTenantId { get; set; }

    /// <summary>
    /// Full HTTPS URL of the bot messaging endpoint that Teams sends call events to.
    /// Format: https://&lt;functionapp&gt;.azurewebsites.us/api/bot-messages
    /// Used for reference and for registering in Teams Admin Center.
    /// </summary>
    [JsonPropertyName("teamsBotMessagingEndpoint")]
    public string? TeamsBotMessagingEndpoint { get; set; }

    /// <summary>
    /// Describes how PSTN numbers are provisioned in Teams Phone System.
    /// CallingPlan — Microsoft provides numbers; no carrier/SBC needed.
    /// OperatorConnect — Carrier connects directly to Teams; carrier manages SBC.
    /// DirectRouting — Customer-managed SBC with Teams Direct Routing.
    /// </summary>
    [JsonPropertyName("teamsPhoneSystemMode")]
    public string TeamsPhoneSystemMode { get; set; } = "CallingPlan";

    /// <summary>
    /// Azure Bot Service resource name (e.g. ivr-dev-bot-xxxx).
    /// Informational — used when registering the bot in Teams Admin Center.
    /// </summary>
    [JsonPropertyName("teamsBotServiceName")]
    public string? TeamsBotServiceName { get; set; }

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "config";
}
