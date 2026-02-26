using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Audio/TTS prompt used in IVR menus.
/// </summary>
public class IvrPrompt
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public PromptType Type { get; set; } = PromptType.Tts;

    [JsonPropertyName("ttsText")]
    public string? TtsText { get; set; }

    [JsonPropertyName("ttsVoice")]
    public string TtsVoice { get; set; } = "en-US-JennyNeural";

    [JsonPropertyName("ttsStyle")]
    public string? TtsStyle { get; set; }

    [JsonPropertyName("audioFileUrl")]
    public string? AudioFileUrl { get; set; }

    [JsonPropertyName("audioBlobName")]
    public string? AudioBlobName { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-US";

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "prompt";
}

public enum PromptType
{
    Tts,
    AudioFile,
    Ssml
}
