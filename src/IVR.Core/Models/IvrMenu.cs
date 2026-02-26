using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Represents an IVR menu node in the call flow tree.
/// </summary>
public class IvrMenu
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("promptId")]
    public string PromptId { get; set; } = string.Empty;

    [JsonPropertyName("menuType")]
    public MenuType MenuType { get; set; } = MenuType.Standard;

    [JsonPropertyName("parentMenuId")]
    public string? ParentMenuId { get; set; }

    [JsonPropertyName("options")]
    public List<MenuOption> Options { get; set; } = new();

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 10;

    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("timeoutPromptId")]
    public string? TimeoutPromptId { get; set; }

    [JsonPropertyName("invalidInputPromptId")]
    public string? InvalidInputPromptId { get; set; }

    [JsonPropertyName("maxRetriesExceededAction")]
    public MenuAction? MaxRetriesExceededAction { get; set; }

    [JsonPropertyName("conditions")]
    public List<MenuCondition> Conditions { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, this menu uses speech recognition instead of DTMF to capture caller input.
    /// The transcript is sent to Azure OpenAI for intent classification and team routing.
    /// </summary>
    [JsonPropertyName("enableSpeechRecognition")]
    public bool EnableSpeechRecognition { get; set; }

    /// <summary>
    /// Prompt played before speech recognition (e.g. "Please briefly describe your issue.").
    /// Only used when EnableSpeechRecognition is true.
    /// </summary>
    [JsonPropertyName("speechRoutingPromptId")]
    public string? SpeechRoutingPromptId { get; set; }

    /// <summary>
    /// IDs of TeamRoutingConfig documents to evaluate the transcript against.
    /// Only used when EnableSpeechRecognition is true.
    /// </summary>
    [JsonPropertyName("teamRoutingConfigIds")]
    public List<string> TeamRoutingConfigIds { get; set; } = new();

    /// <summary>
    /// Fallback action when speech intent cannot be determined.
    /// </summary>
    [JsonPropertyName("speechFallbackAction")]
    public MenuAction? SpeechFallbackAction { get; set; }

    [JsonPropertyName("isRootMenu")]
    public bool IsRootMenu { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "menu";
}

public class MenuOption
{
    [JsonPropertyName("dtmfKey")]
    public string DtmfKey { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("speechKeywords")]
    public List<string> SpeechKeywords { get; set; } = new();

    [JsonPropertyName("action")]
    public MenuAction Action { get; set; } = new();
}

public class MenuAction
{
    [JsonPropertyName("type")]
    public ActionType Type { get; set; }

    [JsonPropertyName("targetMenuId")]
    public string? TargetMenuId { get; set; }

    [JsonPropertyName("transferNumber")]
    public string? TransferNumber { get; set; }

    [JsonPropertyName("queueName")]
    public string? QueueName { get; set; }

    [JsonPropertyName("promptId")]
    public string? PromptId { get; set; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// ID of the DataExtractionConfig to use for AI-powered data extraction
    /// and external system submission. Used with ActionType.SubmitToExternalSystem.
    /// </summary>
    [JsonPropertyName("dataExtractionConfigId")]
    public string? DataExtractionConfigId { get; set; }

    /// <summary>
    /// Avaya CM10 VDN address for TransferToVdn actions.
    /// Can be a SIP URI ("sip:70100@sbc.contoso.com") or E.164 number ("+18005550100").
    /// If null, falls back to SystemConfig.DefaultCm10Vdn.
    /// </summary>
    [JsonPropertyName("vdnAddress")]
    public string? VdnAddress { get; set; }

    [JsonPropertyName("customData")]
    public Dictionary<string, string> CustomData { get; set; } = new();
}

public class MenuCondition
{
    [JsonPropertyName("type")]
    public ConditionType Type { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("targetMenuId")]
    public string? TargetMenuId { get; set; }

    [JsonPropertyName("targetAction")]
    public MenuAction? TargetAction { get; set; }
}

public enum MenuType
{
    Standard,
    Root,
    SubMenu,
    AfterHours,
    Holiday,
    Emergency,
    VipRouting,
    SpeechRouting
}

public enum ActionType
{
    NavigateToMenu,
    TransferToAgent,
    TransferToQueue,
    TransferToNumber,
    PlayPrompt,
    Hangup,
    Voicemail,
    Callback,
    Webhook,
    RepeatMenu,
    TransferToTeam,
    SubmitToExternalSystem,
    /// <summary>
    /// Transfer the call to an Avaya CM10 VDN via SIP.
    /// Uses MenuAction.VdnAddress (or falls back to SystemConfig.DefaultCm10Vdn).
    /// </summary>
    TransferToVdn
}

public enum ConditionType
{
    BusinessHours,
    AniMatch,
    AliRegion,
    CallerType,
    VipStatus,
    Holiday,
    Custom
}
