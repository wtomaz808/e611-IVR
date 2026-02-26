using Azure.Communication.CallAutomation;
using IVR.Core.Interfaces;
using IVR.Core.Models;
using IVR.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IVR.Functions.Functions;

/// <summary>
/// Handles incoming calls from Azure Communication Services.
/// Accepts both Event Grid webhook deliveries and direct HTTP posts (simulator mock mode).
/// This is the entry point for all inbound calls to the IVR system.
/// </summary>
public class IncomingCallHandler
{
    private readonly CallAutomationClient _callClient;
    private readonly ICallFlowEngine _callFlowEngine;
    private readonly AniAliService _aniAliService;
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<IncomingCallHandler> _logger;
    private readonly string _callbackBaseUrl;

    public IncomingCallHandler(
        CallAutomationClient callClient,
        ICallFlowEngine callFlowEngine,
        AniAliService aniAliService,
        ICosmosDbService cosmosDb,
        IConfiguration configuration,
        ILogger<IncomingCallHandler> logger)
    {
        _callClient = callClient;
        _callFlowEngine = callFlowEngine;
        _aniAliService = aniAliService;
        _cosmosDb = cosmosDb;
        _logger = logger;
        _callbackBaseUrl = configuration["CallbackBaseUrl"]
            ?? "http://localhost:7071";
    }

    /// <summary>
    /// HTTP trigger — accepts EventGrid-formatted JSON from either a real
    /// Event Grid subscription or the PSTN Simulator's mock bridge.
    /// </summary>
    [Function("IncomingCallHandler")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "incoming-call")] HttpRequest req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("IncomingCallHandler received request ({Length} bytes)", body.Length);

        JsonElement[] events;
        try
        {
            events = JsonSerializer.Deserialize<JsonElement[]>(body) ?? Array.Empty<JsonElement>();
        }
        catch
        {
            _logger.LogError("Failed to parse request body as JSON array");
            return new BadRequestResult();
        }

        // ─── Handle Event Grid subscription validation ──────────
        foreach (var evt in events)
        {
            var eventType = evt.TryGetProperty("eventType", out var et) ? et.GetString() : null;

            if (eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationCode = evt.GetProperty("data").GetProperty("validationCode").GetString();
                return new OkObjectResult(new { validationResponse = validationCode });
            }

            if (eventType == "Microsoft.Communication.IncomingCall")
            {
                var data = evt.GetProperty("data");
                await HandleIncomingCallAsync(data);
            }
        }

        return new OkResult();
    }

    private async Task HandleIncomingCallAsync(JsonElement data)
    {
        // ─── Extract caller and called numbers ─────────────────────
        var callerNumber = ExtractPhoneNumber(data.GetProperty("from"));
        var calledNumber = ExtractPhoneNumber(data.GetProperty("to"));
        var incomingCallContext = data.GetProperty("incomingCallContext").GetString() ?? "";

        _logger.LogInformation("Call from {CallerNumber} to {CalledNumber}", callerNumber, calledNumber);

        // ─── Step 1: ANI/ALI Lookup ─────────────────────────────────
        var callContext = await _callFlowEngine.BuildCallContextAsync(callerNumber, calledNumber);

        // Check if caller is blocked
        if (callContext.IsBlocked)
        {
            _logger.LogWarning("Blocked caller {CallerNumber} — rejecting call", callerNumber);
            await _callClient.RejectCallAsync(incomingCallContext);
            return;
        }

        // ─── Step 2: Create Call Log ────────────────────────────────
        var callLog = new CallLog
        {
            CallId = callContext.CallId,
            CallerNumber = callerNumber,
            CalledNumber = calledNumber,
            AniData = callContext.AniData,
            AliData = callContext.AliData,
            Status = CallStatus.Ringing,
            StartTime = DateTime.UtcNow
        };
        await _cosmosDb.CreateCallLogAsync(callLog);

        // ─── Step 3: Answer the Call ────────────────────────────────
        var callbackUri = new Uri($"{_callbackBaseUrl}/api/callbacks/{callContext.CallId}");

        var answerOptions = new AnswerCallOptions(incomingCallContext, callbackUri);

        // Configure Cognitive Services for speech recognition
        var cognitiveEndpoint = Environment.GetEnvironmentVariable("CognitiveServicesEndpoint");
        if (!string.IsNullOrEmpty(cognitiveEndpoint))
        {
            answerOptions.CallIntelligenceOptions = new CallIntelligenceOptions
            {
                CognitiveServicesEndpoint = new Uri(cognitiveEndpoint)
            };
        }

        var answerResult = await _callClient.AnswerCallAsync(answerOptions);
        _logger.LogInformation("Call answered successfully. Connection ID: {ConnectionId}",
            answerResult.Value.CallConnection.CallConnectionId);

        // ─── Step 4: Update Call Log ────────────────────────────────
        callLog.Status = CallStatus.InProgress;
        callLog.CorrelationId = answerResult.Value.CallConnection.CallConnectionId;
        await _cosmosDb.UpdateCallLogAsync(callLog);
    }

    /// <summary>
    /// Extract a normalized E.164 phone number from an ACS participant JSON element.
    /// Handles both native ACS numbers (<c>phoneNumber.value</c>) and SIP/direct-routed
    /// numbers that only provide a <c>rawId</c> in SIP URI format.
    /// </summary>
    private static string ExtractPhoneNumber(JsonElement participant)
    {
        // 1. Try the standard phoneNumber.value path (native ACS numbers)
        if (participant.TryGetProperty("phoneNumber", out var phoneNumberProp) &&
            phoneNumberProp.TryGetProperty("value", out var valueProp))
        {
            var value = valueProp.GetString();
            if (!string.IsNullOrEmpty(value))
                return AniAliService.NormalizePhoneNumber(value);
        }

        // 2. Fall back to rawId — may be a SIP URI, tel URI, or ACS rawId
        if (participant.TryGetProperty("rawId", out var rawIdProp))
        {
            var rawId = rawIdProp.GetString();
            if (!string.IsNullOrEmpty(rawId))
                return AniAliService.ExtractPhoneFromUri(rawId);
        }

        return "unknown";
    }
}
