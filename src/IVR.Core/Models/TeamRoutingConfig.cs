using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Defines a team that callers can be routed to based on transcript intent classification.
/// Each config maps a team name, transfer destination, and optional confirmation prompt.
/// </summary>
public class TeamRoutingConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable team name (e.g. "Billing", "Technical Support").
    /// Also used as the intent label for AI classification.
    /// </summary>
    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of what this team handles, used in the AI prompt
    /// to improve classification accuracy.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Phone number or SIP URI to transfer the caller to.
    /// </summary>
    [JsonPropertyName("transferNumber")]
    public string TransferNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional queue name for queue-based routing.
    /// </summary>
    [JsonPropertyName("queueName")]
    public string? QueueName { get; set; }

    /// <summary>
    /// Example phrases/keywords that describe this team's domain.
    /// Fed into the AI prompt to anchor intent classification.
    /// </summary>
    [JsonPropertyName("intentKeywords")]
    public List<string> IntentKeywords { get; set; } = new();

    /// <summary>
    /// Optional prompt played to confirm the detected intent before transferring.
    /// E.g. "It sounds like you need Billing. Is that correct?"
    /// </summary>
    [JsonPropertyName("confirmationPromptId")]
    public string? ConfirmationPromptId { get; set; }

    /// <summary>
    /// Priority when multiple teams could match. Higher = preferred.
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "team-routing";
}

/// <summary>
/// Result of transcript-based intent classification and team routing.
/// </summary>
public class TeamRoutingResult
{
    /// <summary>
    /// The matched team configuration, or null if no match.
    /// </summary>
    public TeamRoutingConfig? MatchedTeam { get; set; }

    /// <summary>
    /// The intent detected by the AI (e.g. "Billing", "Technical Support").
    /// </summary>
    public string DetectedIntent { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score from 0.0 to 1.0.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The original caller transcript.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Whether the confidence was high enough to route without confirmation.
    /// </summary>
    public bool IsHighConfidence => Confidence >= 0.8;

    /// <summary>
    /// Whether any team was matched at all.
    /// </summary>
    public bool IsMatched => MatchedTeam != null;
}
