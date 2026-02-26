using Azure.AI.OpenAI;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace IVR.Functions.Services;

/// <summary>
/// Uses Azure OpenAI to classify a caller's spoken transcript into a team/intent
/// and determine the appropriate routing destination.
/// </summary>
public class TranscriptRoutingService
{
    private readonly ChatClient _chatClient;
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<TranscriptRoutingService> _logger;

    /// <summary>
    /// Minimum confidence threshold for automatic routing without confirmation.
    /// </summary>
    private const double HighConfidenceThreshold = 0.8;

    /// <summary>
    /// Minimum confidence threshold to consider a match at all.
    /// Below this, the fallback action is triggered.
    /// </summary>
    private const double MinConfidenceThreshold = 0.4;

    public TranscriptRoutingService(
        AzureOpenAIClient openAIClient,
        ICosmosDbService cosmosDb,
        IConfiguration configuration,
        ILogger<TranscriptRoutingService> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;

        var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        _chatClient = openAIClient.GetChatClient(deploymentName);
    }

    /// <summary>
    /// Classify the caller's transcript against the given team routing configurations
    /// using Azure OpenAI, and return the routing result.
    /// </summary>
    public async Task<TeamRoutingResult> ClassifyAndRouteAsync(string transcript, List<TeamRoutingConfig> teams)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            _logger.LogWarning("Empty transcript received for routing");
            return new TeamRoutingResult
            {
                Transcript = transcript,
                DetectedIntent = "unknown",
                Confidence = 0
            };
        }

        if (teams.Count == 0)
        {
            _logger.LogWarning("No team routing configs available");
            return new TeamRoutingResult
            {
                Transcript = transcript,
                DetectedIntent = "no_teams_configured",
                Confidence = 0
            };
        }

        try
        {
            var systemPrompt = BuildSystemPrompt(teams);
            var userPrompt = $"Caller said: \"{transcript}\"";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Low temperature for deterministic classification
                MaxOutputTokenCount = 200,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            _logger.LogInformation("Classifying transcript with Azure OpenAI: \"{Transcript}\"", transcript);

            var response = await _chatClient.CompleteChatAsync(chatMessages, options);
            var responseContent = response.Value.Content[0].Text;

            _logger.LogInformation("OpenAI classification response: {Response}", responseContent);

            return ParseClassificationResponse(responseContent, transcript, teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify transcript with Azure OpenAI");
            return new TeamRoutingResult
            {
                Transcript = transcript,
                DetectedIntent = "classification_error",
                Confidence = 0
            };
        }
    }

    /// <summary>
    /// Convenience method: loads team configs from Cosmos DB by IDs and classifies.
    /// </summary>
    public async Task<TeamRoutingResult> ClassifyAndRouteAsync(string transcript, List<string> teamConfigIds)
    {
        var teams = await _cosmosDb.GetTeamRoutingConfigsAsync(teamConfigIds);
        return await ClassifyAndRouteAsync(transcript, teams);
    }

    /// <summary>
    /// Build the system prompt that instructs the AI how to classify caller intent.
    /// </summary>
    private static string BuildSystemPrompt(List<TeamRoutingConfig> teams)
    {
        var teamDescriptions = teams.Select(t =>
        {
            var keywords = t.IntentKeywords.Count > 0 ? $" Keywords: {string.Join(", ", t.IntentKeywords)}" : "";
            var description = !string.IsNullOrEmpty(t.Description) ? $" Description: {t.Description}" : "";
            return $"- \"{t.TeamName}\"{description}{keywords}";
        });

        return $@"You are an IVR call routing assistant. Your job is to classify a caller's spoken request 
into one of the available teams/departments for routing.

Available teams:
{string.Join("\n", teamDescriptions)}

Analyze the caller's statement and respond with a JSON object:
{{
    ""intent"": ""<team name that best matches>"",
    ""confidence"": <number between 0.0 and 1.0>,
    ""reasoning"": ""<brief explanation of why this team was chosen>""
}}

Rules:
- Choose the single best matching team from the list above.
- Set confidence based on how clearly the caller's request matches the team.
- If the caller's request is ambiguous or doesn't clearly match any team, set confidence below 0.4.
- The ""intent"" value MUST exactly match one of the team names listed above.
- Be generous with matching — callers may use informal language.
- If the caller mentions multiple topics, pick the primary/first concern.";
    }

    /// <summary>
    /// Parse the AI's JSON response into a TeamRoutingResult.
    /// </summary>
    private TeamRoutingResult ParseClassificationResponse(string responseJson, string transcript, List<TeamRoutingConfig> teams)
    {
        try
        {
            var classification = JsonSerializer.Deserialize<AiClassificationResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (classification == null)
            {
                _logger.LogWarning("Failed to deserialize AI classification response");
                return new TeamRoutingResult { Transcript = transcript, DetectedIntent = "parse_error", Confidence = 0 };
            }

            // Find matching team by name (case-insensitive)
            var matchedTeam = teams.FirstOrDefault(t =>
                t.TeamName.Equals(classification.Intent, StringComparison.OrdinalIgnoreCase));

            var result = new TeamRoutingResult
            {
                Transcript = transcript,
                DetectedIntent = classification.Intent ?? "unknown",
                Confidence = Math.Clamp(classification.Confidence, 0.0, 1.0),
                MatchedTeam = classification.Confidence >= MinConfidenceThreshold ? matchedTeam : null
            };

            if (result.IsMatched)
            {
                _logger.LogInformation(
                    "Transcript classified as \"{Intent}\" with confidence {Confidence:P0} → routing to {TeamName}",
                    result.DetectedIntent, result.Confidence, result.MatchedTeam!.TeamName);
            }
            else
            {
                _logger.LogInformation(
                    "Transcript classified as \"{Intent}\" with confidence {Confidence:P0} — below threshold, no match",
                    result.DetectedIntent, result.Confidence);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI classification JSON: {Response}", responseJson);
            return new TeamRoutingResult { Transcript = transcript, DetectedIntent = "parse_error", Confidence = 0 };
        }
    }

    /// <summary>
    /// Internal model for deserializing the AI's JSON response.
    /// </summary>
    private class AiClassificationResponse
    {
        public string Intent { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
    }
}
