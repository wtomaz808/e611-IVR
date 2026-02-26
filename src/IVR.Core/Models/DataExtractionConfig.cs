using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Configures how Azure OpenAI extracts structured data fields from a caller's transcript.
/// Each config defines a set of fields to extract and links to the external system + endpoint
/// where the extracted data should be submitted.
///
/// Example: A "Fire Alarm Actions" config would extract fields like:
///   action ("test", "reset", "silence"), systemType ("fire_alarm"), location ("Building 7")
/// and submit them to the Fire Alarm Panel's "put-in-test" endpoint.
/// </summary>
public class DataExtractionConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name (e.g. "Fire Alarm Actions", "Elevator Service Requests").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what kind of data this config extracts.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The fields to extract from the caller's transcript.
    /// </summary>
    [JsonPropertyName("fields")]
    public List<ExtractionField> Fields { get; set; } = new();

    /// <summary>
    /// ID of the ExternalSystemConfig to send the extracted data to.
    /// </summary>
    [JsonPropertyName("externalSystemId")]
    public string ExternalSystemId { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint action name on the external system to invoke
    /// (must match an ExternalSystemEndpoint.ActionName).
    /// </summary>
    [JsonPropertyName("endpointActionName")]
    public string EndpointActionName { get; set; } = string.Empty;

    /// <summary>
    /// Optional: additional context instructions for the AI to improve extraction accuracy.
    /// E.g. "The caller is reporting a fire alarm system action. Locations are typically
    /// building numbers or floor names."
    /// </summary>
    [JsonPropertyName("aiContextInstructions")]
    public string? AiContextInstructions { get; set; }

    /// <summary>
    /// Prompt played to confirm the extracted data before submission.
    /// If null, data is submitted without confirmation.
    /// </summary>
    [JsonPropertyName("confirmationPromptId")]
    public string? ConfirmationPromptId { get; set; }

    /// <summary>
    /// Whether the caller must confirm (press 1) before the data is submitted.
    /// </summary>
    [JsonPropertyName("requireCallerConfirmation")]
    public bool RequireCallerConfirmation { get; set; }

    /// <summary>
    /// TTS template used to read back extracted data for confirmation.
    /// Supports {{fieldName}} placeholders. Example:
    /// "I'll put fire alarms at {{location}} in {{action}} mode. Press 1 to confirm or 2 to cancel."
    /// </summary>
    [JsonPropertyName("confirmationTtsTemplate")]
    public string? ConfirmationTtsTemplate { get; set; }

    /// <summary>
    /// Prompt played after successful submission.
    /// </summary>
    [JsonPropertyName("successPromptId")]
    public string? SuccessPromptId { get; set; }

    /// <summary>
    /// TTS template for success message. Supports {{confirmationValue}} from the external
    /// system's response. Example: "Done. Your reference number is {{confirmationValue}}."
    /// </summary>
    [JsonPropertyName("successTtsTemplate")]
    public string? SuccessTtsTemplate { get; set; }

    /// <summary>
    /// Prompt played when the external system call fails.
    /// </summary>
    [JsonPropertyName("failurePromptId")]
    public string? FailurePromptId { get; set; }

    /// <summary>
    /// Action to take after submission (e.g. transfer to team, return to menu, hang up).
    /// </summary>
    [JsonPropertyName("postSubmitAction")]
    public MenuAction? PostSubmitAction { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "data-extraction";
}

/// <summary>
/// A single field to extract from the caller's transcript.
/// </summary>
public class ExtractionField
{
    /// <summary>
    /// Machine-readable field name used in payload templates (e.g. "action", "location", "building").
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the field.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Description to guide the AI on what to extract.
    /// E.g. "The action the caller wants to perform on the fire alarm system."
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Data type hint for the AI.
    /// </summary>
    [JsonPropertyName("dataType")]
    public ExtractionFieldType DataType { get; set; } = ExtractionFieldType.String;

    /// <summary>
    /// Whether this field is required. If required and the AI can't extract it,
    /// the caller will be prompted to provide it.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    /// <summary>
    /// Default value to use if the field is not found in the transcript.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Allowed values for this field. If set, the AI is constrained to these options.
    /// E.g. ["test", "reset", "silence", "acknowledge"] for a fire alarm action field.
    /// </summary>
    [JsonPropertyName("validValues")]
    public List<string> ValidValues { get; set; } = new();

    /// <summary>
    /// Aliases or synonyms the AI should map to valid values.
    /// E.g. { "put in test": "test", "testing": "test", "quiet": "silence" }
    /// </summary>
    [JsonPropertyName("valueAliases")]
    public Dictionary<string, string> ValueAliases { get; set; } = new();
}

/// <summary>
/// Result of AI data extraction from a transcript.
/// </summary>
public class DataExtractionResult
{
    /// <summary>
    /// The extracted field values, keyed by field name.
    /// </summary>
    public Dictionary<string, string> ExtractedFields { get; set; } = new();

    /// <summary>
    /// Fields that were required but could not be extracted.
    /// </summary>
    public List<string> MissingRequiredFields { get; set; } = new();

    /// <summary>
    /// Whether all required fields were successfully extracted.
    /// </summary>
    public bool IsComplete => MissingRequiredFields.Count == 0;

    /// <summary>
    /// Overall confidence of the extraction (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The original transcript.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;
}

/// <summary>
/// Result of submitting extracted data to an external system.
/// </summary>
public class ExternalSystemSubmissionResult
{
    /// <summary>
    /// Whether the submission succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code from the external system.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Confirmation/reference value extracted from the response
    /// (e.g. a ticket ID or reference number).
    /// </summary>
    public string? ConfirmationValue { get; set; }

    /// <summary>
    /// Full response body from the external system.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Error message if the submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Name of the external system that was called.
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// The endpoint action that was invoked.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the submission attempt.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum ExtractionFieldType
{
    String,
    Number,
    Boolean,
    Date,
    Enum
}
