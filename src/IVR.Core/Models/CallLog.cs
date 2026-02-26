using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Record of an individual call through the IVR system.
/// </summary>
public class CallLog
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("callerNumber")]
    public string CallerNumber { get; set; } = string.Empty;

    [JsonPropertyName("calledNumber")]
    public string CalledNumber { get; set; } = string.Empty;

    [JsonPropertyName("aniData")]
    public AniRecord? AniData { get; set; }

    [JsonPropertyName("aliData")]
    public AliRecord? AliData { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds { get; set; }

    [JsonPropertyName("status")]
    public CallStatus Status { get; set; } = CallStatus.Ringing;

    [JsonPropertyName("disposition")]
    public CallDisposition Disposition { get; set; } = CallDisposition.Unknown;

    [JsonPropertyName("menuPath")]
    public List<MenuPathEntry> MenuPath { get; set; } = new();

    [JsonPropertyName("transferredTo")]
    public string? TransferredTo { get; set; }

    [JsonPropertyName("queueName")]
    public string? QueueName { get; set; }

    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    [JsonPropertyName("recordingUrl")]
    public string? RecordingUrl { get; set; }

    /// <summary>
    /// The caller's spoken transcript captured via speech recognition.
    /// </summary>
    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }

    /// <summary>
    /// The intent detected by the AI from the transcript (e.g. "Billing").
    /// </summary>
    [JsonPropertyName("detectedIntent")]
    public string? DetectedIntent { get; set; }

    /// <summary>
    /// The team the call was routed to based on transcript analysis.
    /// </summary>
    [JsonPropertyName("routedToTeam")]
    public string? RoutedToTeam { get; set; }

    /// <summary>
    /// Confidence score of the intent classification (0.0 - 1.0).
    /// </summary>
    [JsonPropertyName("intentConfidence")]
    public double? IntentConfidence { get; set; }

    /// <summary>
    /// Structured data extracted from the caller's transcript by AI.
    /// </summary>
    [JsonPropertyName("extractedData")]
    public Dictionary<string, string>? ExtractedData { get; set; }

    /// <summary>
    /// Results of submissions to external systems during this call.
    /// </summary>
    [JsonPropertyName("externalSystemResults")]
    public List<ExternalSystemSubmissionResult> ExternalSystemResults { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => StartTime.ToString("yyyy-MM");
}

public class MenuPathEntry
{
    [JsonPropertyName("menuId")]
    public string MenuId { get; set; } = string.Empty;

    [JsonPropertyName("menuName")]
    public string MenuName { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum CallStatus
{
    Ringing,
    InProgress,
    InQueue,
    Transferred,
    Completed,
    Failed,
    Abandoned
}

public enum CallDisposition
{
    Unknown,
    Completed,
    TransferredToAgent,
    TransferredToQueue,
    TransferredToExternal,
    TransferredToVdn,
    Voicemail,
    Abandoned,
    SystemError,
    CallerHangup
}
