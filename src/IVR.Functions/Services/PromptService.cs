using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;

namespace IVR.Functions.Services;

/// <summary>
/// Service for resolving and building IVR prompts for playback.
/// </summary>
public class PromptService
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<PromptService> _logger;

    public PromptService(ICosmosDbService cosmosDb, IBlobStorageService blobStorage, ILogger<PromptService> logger)
    {
        _cosmosDb = cosmosDb;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <summary>
    /// Resolve a prompt by ID and return the playable content (TTS text or audio URL).
    /// </summary>
    public async Task<PromptContent> ResolvePromptAsync(string promptId)
    {
        var prompt = await _cosmosDb.GetPromptAsync(promptId);

        if (prompt == null)
        {
            _logger.LogWarning("Prompt {PromptId} not found, using fallback", promptId);
            return new PromptContent
            {
                Type = PromptType.Tts,
                Text = "We're sorry, an error has occurred. Please try again later.",
                Voice = "en-US-JennyNeural"
            };
        }

        return prompt.Type switch
        {
            PromptType.Tts => new PromptContent
            {
                Type = PromptType.Tts,
                Text = prompt.TtsText ?? "Please wait.",
                Voice = prompt.TtsVoice,
                Language = prompt.Language
            },
            PromptType.AudioFile => new PromptContent
            {
                Type = PromptType.AudioFile,
                AudioUrl = prompt.AudioBlobName != null
                    ? await _blobStorage.GetAudioUrlAsync(prompt.AudioBlobName)
                    : prompt.AudioFileUrl ?? throw new InvalidOperationException($"Audio prompt {promptId} has no file"),
                Language = prompt.Language
            },
            PromptType.Ssml => new PromptContent
            {
                Type = PromptType.Ssml,
                SsmlContent = prompt.TtsText ?? throw new InvalidOperationException($"SSML prompt {promptId} has no content"),
                Language = prompt.Language
            },
            _ => throw new NotSupportedException($"Prompt type {prompt.Type} is not supported")
        };
    }

    /// <summary>
    /// Build the SSML for a TTS prompt with optional voice and style.
    /// </summary>
    public string BuildSsml(string text, string voice = "en-US-JennyNeural", string? style = null)
    {
        var expressAs = style != null
            ? $"<mstts:express-as style=\"{style}\">{text}</mstts:express-as>"
            : text;

        return $@"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis""
    xmlns:mstts=""https://www.w3.org/2001/mstts""
    xml:lang=""en-US"">
  <voice name=""{voice}"">
    {expressAs}
  </voice>
</speak>";
    }
}

public class PromptContent
{
    public PromptType Type { get; set; }
    public string? Text { get; set; }
    public string? Voice { get; set; }
    public string? AudioUrl { get; set; }
    public string? SsmlContent { get; set; }
    public string Language { get; set; } = "en-US";
}
