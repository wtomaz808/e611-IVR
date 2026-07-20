using Azure.Storage.Blobs;
using IVR.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace IVR.Functions.Services;

/// <summary>
/// Converts TTS text or SSML to a WAV audio file and stores it in Blob Storage,
/// returning a short-lived SAS URL that the Microsoft Graph playPrompt API can fetch.
///
/// Graph's playPrompt requires a publicly accessible audio URL — it cannot accept
/// raw TTS text the way ACS Call Automation could. This service bridges that gap
/// by calling the Azure Cognitive Services Speech TTS REST API directly and caching
/// the result in Blob Storage keyed by a hash of the text + voice so identical
/// prompts are not re-generated on every call.
/// </summary>
public class TtsGenerationService
{
    // WAV format Teams Calling Bot accepts: PCM 16-bit 16kHz mono
    private const string OutputFormat = "riff-16khz-16bit-mono-pcm";
    private const string TtsBlobPrefix = "tts-cache/";
    private static readonly TimeSpan SasExpiry = TimeSpan.FromHours(2);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBlobStorageService _blobStorage;
    private readonly string _ttsEndpoint;
    private readonly string _ttsApiKey;
    private readonly ILogger<TtsGenerationService> _logger;

    public TtsGenerationService(
        IHttpClientFactory httpClientFactory,
        IBlobStorageService blobStorage,
        IConfiguration configuration,
        ILogger<TtsGenerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _blobStorage = blobStorage;
        _logger = logger;

        // Endpoint: {CognitiveServicesEndpoint}tts/cognitiveservices/v1
        // Commercial: https://<custom>.cognitiveservices.azure.com/tts/cognitiveservices/v1
        // Gov (GCC High): https://<custom>.cognitiveservices.azure.us/tts/cognitiveservices/v1
        var baseEndpoint = configuration["CognitiveServicesEndpoint"]?.TrimEnd('/') ?? "";
        _ttsEndpoint = $"{baseEndpoint}/tts/cognitiveservices/v1";
        _ttsApiKey = configuration["CognitiveServicesKey"] ?? "";
    }

    /// <summary>
    /// Generates or retrieves cached TTS audio for <paramref name="text"/>
    /// and returns a time-limited SAS URL suitable for Graph's playPrompt.
    /// </summary>
    /// <param name="text">Plain text to synthesize.</param>
    /// <param name="voice">Neural voice name, e.g. "en-US-JennyNeural".</param>
    /// <param name="language">BCP-47 language tag, e.g. "en-US".</param>
    public async Task<string> GetAudioUrlAsync(
        string text,
        string voice = "en-US-JennyNeural",
        string language = "en-US")
    {
        var ssml = BuildSsml(text, voice, language);
        return await GetAudioUrlFromSsmlAsync(ssml, voice);
    }

    /// <summary>
    /// Generates or retrieves cached TTS audio for <paramref name="ssml"/>
    /// and returns a time-limited SAS URL.
    /// </summary>
    public async Task<string> GetAudioUrlFromSsmlAsync(string ssml, string voice = "en-US-JennyNeural")
    {
        // Content-address the blob so identical prompts reuse the same file
        var blobName = $"{TtsBlobPrefix}{ComputeHash(ssml)}.wav";

        // Check if already cached
        var existing = await _blobStorage.GetAudioUrlAsync(blobName, SasExpiry);
        if (!string.IsNullOrEmpty(existing))
        {
            _logger.LogDebug("TTS cache hit for blob {BlobName}", blobName);
            return existing;
        }

        // Generate via Cognitive Services TTS REST API
        _logger.LogInformation("Generating TTS audio for voice={Voice} ({SsmlLength} chars)", voice, ssml.Length);

        var audioBytes = await SynthesizeSpeechAsync(ssml);

        using var stream = new MemoryStream(audioBytes);
        await _blobStorage.UploadAudioAsync(stream, blobName, "audio/wav");

        var url = await _blobStorage.GetAudioUrlAsync(blobName, SasExpiry);
        _logger.LogInformation("TTS audio cached at {BlobName}", blobName);
        return url;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private async Task<byte[]> SynthesizeSpeechAsync(string ssml)
    {
        if (string.IsNullOrEmpty(_ttsApiKey))
            throw new InvalidOperationException(
                "CognitiveServicesKey is not configured. TTS generation unavailable.");

        var client = _httpClientFactory.CreateClient("TtsService");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _ttsApiKey);
        client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", OutputFormat);
        client.DefaultRequestHeaders.Add("User-Agent", "IVR-TeamsBot/1.0");

        var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        var response = await client.PostAsync(_ttsEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"TTS API returned {(int)response.StatusCode}: {body}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static string BuildSsml(string text, string voice, string language)
    {
        // Escape XML special characters in the text
        var escaped = System.Security.SecurityElement.Escape(text) ?? text;

        return $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis"
                   xmlns:mstts="https://www.w3.org/2001/mstts"
                   xml:lang="{language}">
              <voice name="{voice}">
                <mstts:express-as style="customerservice">
                  {escaped}
                </mstts:express-as>
              </voice>
            </speak>
            """;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
