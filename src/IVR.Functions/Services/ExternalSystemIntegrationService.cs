using Azure.AI.OpenAI;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IVR.Functions.Services;

/// <summary>
/// Extracts structured data from caller transcripts using Azure OpenAI,
/// then submits that data to configured external systems (fire alarm panels,
/// CAD systems, work order platforms, etc.) via REST APIs.
/// </summary>
public class ExternalSystemIntegrationService
{
    private readonly ChatClient _chatClient;
    private readonly ICosmosDbService _cosmosDb;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalSystemIntegrationService> _logger;

    public ExternalSystemIntegrationService(
        AzureOpenAIClient openAIClient,
        ICosmosDbService cosmosDb,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExternalSystemIntegrationService> logger)
    {
        _cosmosDb = cosmosDb;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        _chatClient = openAIClient.GetChatClient(deploymentName);
    }

    // ─── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Full pipeline: extract structured data from the transcript using AI,
    /// then submit it to the configured external system.
    /// </summary>
    public async Task<ExternalSystemSubmissionResult> ExtractAndSubmitAsync(
        string transcript,
        string dataExtractionConfigId,
        string callerNumber,
        Dictionary<string, string>? additionalContext = null)
    {
        // Load the data extraction config
        var extractionConfig = await _cosmosDb.GetDataExtractionConfigAsync(dataExtractionConfigId);
        if (extractionConfig == null)
        {
            _logger.LogError("DataExtractionConfig {Id} not found", dataExtractionConfigId);
            return new ExternalSystemSubmissionResult
            {
                Success = false,
                ErrorMessage = $"Data extraction config '{dataExtractionConfigId}' not found"
            };
        }

        // Load the external system config
        var externalSystem = await _cosmosDb.GetExternalSystemConfigAsync(extractionConfig.ExternalSystemId);
        if (externalSystem == null || !externalSystem.IsActive)
        {
            _logger.LogError("ExternalSystemConfig {Id} not found or inactive", extractionConfig.ExternalSystemId);
            return new ExternalSystemSubmissionResult
            {
                Success = false,
                ErrorMessage = $"External system '{extractionConfig.ExternalSystemId}' not found or inactive"
            };
        }

        // Step 1: Extract structured data from the transcript
        var extractionResult = await ExtractDataAsync(transcript, extractionConfig);

        if (!extractionResult.IsComplete)
        {
            _logger.LogWarning("Incomplete data extraction. Missing fields: {Fields}",
                string.Join(", ", extractionResult.MissingRequiredFields));

            return new ExternalSystemSubmissionResult
            {
                Success = false,
                SystemName = externalSystem.SystemName,
                ActionName = extractionConfig.EndpointActionName,
                ErrorMessage = $"Could not extract required fields: {string.Join(", ", extractionResult.MissingRequiredFields)}"
            };
        }

        // Inject caller number and any additional context into extracted fields
        extractionResult.ExtractedFields["callerNumber"] = callerNumber;
        extractionResult.ExtractedFields["timestamp"] = DateTime.UtcNow.ToString("o");
        if (additionalContext != null)
        {
            foreach (var kv in additionalContext)
                extractionResult.ExtractedFields.TryAdd(kv.Key, kv.Value);
        }

        // Step 2: Submit to the external system
        return await SubmitToExternalSystemAsync(externalSystem, extractionConfig.EndpointActionName, extractionResult.ExtractedFields);
    }

    /// <summary>
    /// Extract-only: parse structured data from a transcript without submitting.
    /// Useful for confirmation flows where you show the data to the caller first.
    /// </summary>
    public async Task<DataExtractionResult> ExtractDataAsync(string transcript, string dataExtractionConfigId)
    {
        var config = await _cosmosDb.GetDataExtractionConfigAsync(dataExtractionConfigId);
        if (config == null)
        {
            return new DataExtractionResult
            {
                Transcript = transcript,
                Confidence = 0,
                MissingRequiredFields = new List<string> { "config_not_found" }
            };
        }

        return await ExtractDataAsync(transcript, config);
    }

    // ─── AI Data Extraction ────────────────────────────────────────

    /// <summary>
    /// Use Azure OpenAI to extract structured fields from the transcript.
    /// </summary>
    private async Task<DataExtractionResult> ExtractDataAsync(string transcript, DataExtractionConfig config)
    {
        _logger.LogInformation("Extracting data from transcript for config \"{Name}\": \"{Transcript}\"",
            config.Name, transcript);

        try
        {
            var systemPrompt = BuildExtractionPrompt(config);
            var userPrompt = $"Caller said: \"{transcript}\"";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 500,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(chatMessages, options);
            var responseContent = response.Value.Content[0].Text;

            _logger.LogInformation("AI extraction response: {Response}", responseContent);

            return ParseExtractionResponse(responseContent, transcript, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI data extraction failed for config {ConfigId}", config.Id);
            return new DataExtractionResult
            {
                Transcript = transcript,
                Confidence = 0,
                MissingRequiredFields = config.Fields.Where(f => f.Required).Select(f => f.FieldName).ToList()
            };
        }
    }

    /// <summary>
    /// Build the system prompt that instructs the AI how to extract fields.
    /// </summary>
    private static string BuildExtractionPrompt(DataExtractionConfig config)
    {
        var fieldsDescription = config.Fields.Select(f =>
        {
            var parts = new List<string> { $"\"{f.FieldName}\": {f.Description}" };
            if (f.ValidValues.Count > 0)
                parts.Add($"Allowed values: [{string.Join(", ", f.ValidValues.Select(v => $"\"{v}\""))}]");
            if (f.ValueAliases.Count > 0)
                parts.Add($"Synonyms: {string.Join(", ", f.ValueAliases.Select(kv => $"\"{kv.Key}\"→\"{kv.Value}\""))}");
            if (f.Required)
                parts.Add("(REQUIRED)");
            if (f.DefaultValue != null)
                parts.Add($"Default: \"{f.DefaultValue}\"");
            return $"  - {string.Join(". ", parts)}";
        });

        var contextInstructions = !string.IsNullOrEmpty(config.AiContextInstructions)
            ? $"\nContext: {config.AiContextInstructions}\n"
            : "";

        return $@"You are a data extraction assistant for an IVR system. Analyze the caller's spoken statement
and extract the following structured fields.
{contextInstructions}
Fields to extract:
{string.Join("\n", fieldsDescription)}

Respond with a JSON object:
{{
    ""fields"": {{
        ""<fieldName>"": ""<extracted value>"",
        ...
    }},
    ""confidence"": <number between 0.0 and 1.0>,
    ""reasoning"": ""<brief explanation>""
}}

Rules:
- Extract values exactly as the caller stated, then normalize to the valid values list if provided.
- Use synonym/alias mappings when the caller uses informal language.
- If a required field cannot be determined from the statement, set its value to null.
- Be generous with interpretation — callers speak informally.
- For location fields, preserve the caller's original phrasing (e.g. ""Building 7"", ""3rd floor"").
- Set confidence based on how clearly all fields could be extracted.";
    }

    /// <summary>
    /// Parse the AI's extraction response into a DataExtractionResult.
    /// </summary>
    private DataExtractionResult ParseExtractionResponse(string responseJson, string transcript, DataExtractionConfig config)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<AiExtractionResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.Fields == null)
            {
                return new DataExtractionResult
                {
                    Transcript = transcript,
                    Confidence = 0,
                    MissingRequiredFields = config.Fields.Where(f => f.Required).Select(f => f.FieldName).ToList()
                };
            }

            var result = new DataExtractionResult
            {
                Transcript = transcript,
                Confidence = Math.Clamp(parsed.Confidence, 0.0, 1.0)
            };

            // Process each configured field
            foreach (var field in config.Fields)
            {
                if (parsed.Fields.TryGetValue(field.FieldName, out var value) && !string.IsNullOrEmpty(value))
                {
                    result.ExtractedFields[field.FieldName] = value;
                }
                else if (field.DefaultValue != null)
                {
                    result.ExtractedFields[field.FieldName] = field.DefaultValue;
                }
                else if (field.Required)
                {
                    result.MissingRequiredFields.Add(field.FieldName);
                }
            }

            _logger.LogInformation("Extracted {FieldCount} fields with confidence {Confidence:P0}. Missing: [{Missing}]",
                result.ExtractedFields.Count, result.Confidence,
                string.Join(", ", result.MissingRequiredFields));

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI extraction JSON: {Response}", responseJson);
            return new DataExtractionResult
            {
                Transcript = transcript,
                Confidence = 0,
                MissingRequiredFields = config.Fields.Where(f => f.Required).Select(f => f.FieldName).ToList()
            };
        }
    }

    // ─── External System HTTP Dispatch ─────────────────────────────

    /// <summary>
    /// Submit extracted data to an external system's endpoint.
    /// </summary>
    private async Task<ExternalSystemSubmissionResult> SubmitToExternalSystemAsync(
        ExternalSystemConfig system,
        string actionName,
        Dictionary<string, string> data)
    {
        var endpoint = system.Endpoints.FirstOrDefault(e =>
            e.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));

        if (endpoint == null)
        {
            _logger.LogError("Endpoint '{ActionName}' not found on system '{SystemName}'", actionName, system.SystemName);
            return new ExternalSystemSubmissionResult
            {
                Success = false,
                SystemName = system.SystemName,
                ActionName = actionName,
                ErrorMessage = $"Endpoint '{actionName}' not found on system '{system.SystemName}'"
            };
        }

        // Build the URL with placeholder substitution
        var urlPath = ReplacePlaceholders(endpoint.UrlPath, data);
        var fullUrl = $"{system.BaseUrl.TrimEnd('/')}/{urlPath.TrimStart('/')}";

        // Build the request body
        string? requestBody = null;
        if (endpoint.PayloadTemplate != null)
        {
            requestBody = ReplacePlaceholders(endpoint.PayloadTemplate, data);
        }
        else
        {
            // Default: send all extracted data as JSON
            requestBody = JsonSerializer.Serialize(data);
        }

        _logger.LogInformation("Submitting to {SystemName}/{ActionName}: {Method} {Url}",
            system.SystemName, actionName, endpoint.HttpMethod, fullUrl);

        // Execute with retry policy
        return await ExecuteWithRetryAsync(system, endpoint, fullUrl, requestBody, data);
    }

    /// <summary>
    /// Execute the HTTP request with retry logic.
    /// </summary>
    private async Task<ExternalSystemSubmissionResult> ExecuteWithRetryAsync(
        ExternalSystemConfig system,
        ExternalSystemEndpoint endpoint,
        string url,
        string? body,
        Dictionary<string, string> data)
    {
        var retryPolicy = system.RetryPolicy;
        var delayMs = retryPolicy.InitialDelayMs;

        for (int attempt = 0; attempt <= retryPolicy.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Retry attempt {Attempt}/{Max} for {SystemName}/{ActionName}",
                        attempt, retryPolicy.MaxRetries, system.SystemName, endpoint.ActionName);
                    await Task.Delay(delayMs);
                    delayMs = (int)(delayMs * retryPolicy.BackoffMultiplier);
                }

                var result = await ExecuteHttpRequestAsync(system, endpoint, url, body, data);

                if (result.Success || attempt == retryPolicy.MaxRetries)
                    return result;

                // Only retry on server errors (5xx) or timeout
                if (result.StatusCode < 500 && result.StatusCode != 408 && result.StatusCode != 429)
                    return result;
            }
            catch (TaskCanceledException) when (attempt < retryPolicy.MaxRetries)
            {
                _logger.LogWarning("Request timed out for {SystemName}/{ActionName}, will retry",
                    system.SystemName, endpoint.ActionName);
            }
            catch (HttpRequestException ex) when (attempt < retryPolicy.MaxRetries)
            {
                _logger.LogWarning(ex, "HTTP error for {SystemName}/{ActionName}, will retry",
                    system.SystemName, endpoint.ActionName);
            }
        }

        return new ExternalSystemSubmissionResult
        {
            Success = false,
            SystemName = system.SystemName,
            ActionName = endpoint.ActionName,
            ErrorMessage = "All retry attempts exhausted"
        };
    }

    /// <summary>
    /// Execute a single HTTP request to the external system.
    /// </summary>
    private async Task<ExternalSystemSubmissionResult> ExecuteHttpRequestAsync(
        ExternalSystemConfig system,
        ExternalSystemEndpoint endpoint,
        string url,
        string? body,
        Dictionary<string, string> data)
    {
        var client = _httpClientFactory.CreateClient("ExternalSystems");
        client.Timeout = TimeSpan.FromSeconds(system.TimeoutSeconds);

        var request = new HttpRequestMessage(new HttpMethod(endpoint.HttpMethod), url);

        // Set request body
        if (body != null && endpoint.HttpMethod.ToUpperInvariant() != "GET")
        {
            request.Content = new StringContent(body, Encoding.UTF8, endpoint.ContentType);
        }

        // Apply custom headers
        foreach (var header in system.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Apply authentication
        ApplyAuthentication(request, system);

        // Send the request
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("External system response: {StatusCode} from {SystemName}/{ActionName}",
            (int)response.StatusCode, system.SystemName, endpoint.ActionName);

        var result = new ExternalSystemSubmissionResult
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            ResponseBody = responseBody,
            SystemName = system.SystemName,
            ActionName = endpoint.ActionName,
            Timestamp = DateTime.UtcNow
        };

        if (!response.IsSuccessStatusCode)
        {
            result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}";
            _logger.LogWarning("External system error: {StatusCode} {Body}",
                (int)response.StatusCode, responseBody);
        }
        else
        {
            // Extract confirmation value from response if configured
            result.ConfirmationValue = ExtractConfirmationValue(responseBody, endpoint.ResponseConfirmationField);

            if (result.ConfirmationValue != null)
            {
                _logger.LogInformation("Confirmation value extracted: {Value}", result.ConfirmationValue);
            }
        }

        return result;
    }

    /// <summary>
    /// Apply authentication to the HTTP request based on the system's auth config.
    /// </summary>
    private static void ApplyAuthentication(HttpRequestMessage request, ExternalSystemConfig system)
    {
        switch (system.AuthType)
        {
            case ExternalAuthType.ApiKey:
                if (system.AuthConfig.ApiKeyHeaderName != null && system.AuthConfig.ApiKeyValue != null)
                {
                    request.Headers.TryAddWithoutValidation(
                        system.AuthConfig.ApiKeyHeaderName,
                        system.AuthConfig.ApiKeyValue);
                }
                break;

            case ExternalAuthType.BearerToken:
                if (system.AuthConfig.BearerToken != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", system.AuthConfig.BearerToken);
                }
                break;

            case ExternalAuthType.BasicAuth:
                if (system.AuthConfig.Username != null && system.AuthConfig.Password != null)
                {
                    var credentials = Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{system.AuthConfig.Username}:{system.AuthConfig.Password}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case ExternalAuthType.None:
            default:
                break;
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Replace {{placeholder}} tokens in a template string with values from the data dictionary.
    /// </summary>
    private static string ReplacePlaceholders(string template, Dictionary<string, string> data)
    {
        return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
        {
            var key = match.Groups[1].Value;
            return data.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <summary>
    /// Extract a confirmation value from a JSON response using a simple dot-path expression
    /// (e.g. "ticketId", "data.referenceNumber").
    /// </summary>
    private static string? ExtractConfirmationValue(string responseBody, string? fieldPath)
    {
        if (string.IsNullOrEmpty(fieldPath) || string.IsNullOrEmpty(responseBody))
            return null;

        try
        {
            // Remove leading $. if present (JSON path notation)
            fieldPath = fieldPath.TrimStart('$', '.');

            var doc = JsonDocument.Parse(responseBody);
            var element = doc.RootElement;

            foreach (var segment in fieldPath.Split('.'))
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(segment, out var child))
                {
                    element = child;
                }
                else
                {
                    return null;
                }
            }

            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                _ => element.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Build a TTS confirmation message by replacing {{placeholder}} tokens
    /// with extracted data and the confirmation value.
    /// </summary>
    public static string BuildConfirmationMessage(
        string template,
        Dictionary<string, string> extractedData,
        string? confirmationValue = null)
    {
        var data = new Dictionary<string, string>(extractedData);
        if (confirmationValue != null)
            data["confirmationValue"] = confirmationValue;

        return ReplacePlaceholders(template, data);
    }

    /// <summary>
    /// Internal model for deserializing the AI's extraction JSON response.
    /// </summary>
    private class AiExtractionResponse
    {
        public Dictionary<string, string>? Fields { get; set; }
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
    }
}
