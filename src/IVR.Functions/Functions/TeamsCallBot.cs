using IVR.Core.Interfaces;
using IVR.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace IVR.Functions.Functions;

/// <summary>
/// Teams Calling Bot — single HTTP entry point for all call events from Microsoft Teams.
///
/// Teams delivers every call lifecycle event (incoming call, connected, tone received,
/// play completed, call ended) as an HTTP POST to this endpoint via Azure Bot Service.
/// The Bot Framework CloudAdapter validates the JWT, then dispatches to the
/// handler methods below.
///
/// Call control commands (answer, playPrompt, recordResponse, transfer, hangup)
/// are issued back to Teams via the Microsoft Graph Calling API.
///
/// IMPLEMENTATION STATUS: Stub — full implementation in Step 4 of Teams integration.
/// </summary>
public class TeamsCallBot
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly GraphServiceClient? _graphClient;
    private readonly ICallFlowEngine _callFlowEngine;
    private readonly AniAliService _aniAliService;
    private readonly PromptService _promptService;
    private readonly TranscriptRoutingService _transcriptRouting;
    private readonly ExternalSystemIntegrationService _externalIntegration;
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<TeamsCallBot> _logger;

    public TeamsCallBot(
        IBotFrameworkHttpAdapter adapter,
        GraphServiceClient? graphClient,
        ICallFlowEngine callFlowEngine,
        AniAliService aniAliService,
        PromptService promptService,
        TranscriptRoutingService transcriptRouting,
        ExternalSystemIntegrationService externalIntegration,
        ICosmosDbService cosmosDb,
        ILogger<TeamsCallBot> logger)
    {
        _adapter         = adapter;
        _graphClient     = graphClient;
        _callFlowEngine  = callFlowEngine;
        _aniAliService   = aniAliService;
        _promptService   = promptService;
        _transcriptRouting = transcriptRouting;
        _externalIntegration = externalIntegration;
        _cosmosDb        = cosmosDb;
        _logger          = logger;
    }

    /// <summary>
    /// Azure Function HTTP trigger — all Teams call events arrive here.
    /// The CloudAdapter validates the Bearer token from Bot Framework and
    /// dispatches activities to the appropriate handler.
    /// Route: POST /api/bot-messages
    /// </summary>
    [Function("TeamsCallBot")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bot-messages")]
        HttpRequest req)
    {
        _logger.LogInformation("TeamsCallBot received activity ({ContentLength} bytes)", req.ContentLength);

        // TODO (Step 4): Replace this stub with full Bot Framework dispatch.
        // The adapter validates the JWT, deserializes the activity, and calls
        // the appropriate On*Async handler below.
        //
        // await _adapter.ProcessAsync(req, req.HttpContext.Response, this, CancellationToken.None);

        _logger.LogWarning("TeamsCallBot stub — full implementation pending (Step 4)");
        return new OkResult();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Handler stubs — implemented in Step 4
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Called when Teams delivers an incoming PSTN call to the bot.</summary>
    private Task OnIncomingCallAsync(string callId, string callerNumber, string calledNumber)
    {
        // TODO (Step 4):
        // 1. AniAliService lookup
        // 2. Blocked check
        // 3. Create CallLog
        // 4. GraphClient: POST /communications/calls/{callId}/answer
        throw new NotImplementedException("Implemented in Step 4");
    }

    /// <summary>Called when the call is connected after answer.</summary>
    private Task OnCallConnectedAsync(string callId)
    {
        // TODO (Step 4):
        // 1. Resolve root menu via CallFlowEngine
        // 2. GraphClient: POST /communications/calls/{callId}/playPrompt
        // 3. GraphClient: POST /communications/calls/{callId}/recordResponse
        throw new NotImplementedException("Implemented in Step 4");
    }

    /// <summary>Called when a DTMF tone is received from the caller.</summary>
    private Task OnToneReceivedAsync(string callId, string tone)
    {
        // TODO (Step 4):
        // 1. CallFlowEngine.ProcessInputAsync(menuId, tone)
        // 2. Execute resulting action (navigate / transfer / hangup)
        throw new NotImplementedException("Implemented in Step 4");
    }

    /// <summary>Called when speech recognition completes.</summary>
    private Task OnSpeechResultAsync(string callId, string transcript)
    {
        // TODO (Step 4):
        // 1. TranscriptRoutingService.ClassifyAndRouteAsync(transcript, teamConfigIds)
        // 2. Transfer to matched team or fallback
        throw new NotImplementedException("Implemented in Step 4");
    }

    /// <summary>Called when the call is disconnected or transferred away.</summary>
    private Task OnCallDisconnectedAsync(string callId)
    {
        // TODO (Step 4):
        // 1. Finalize CallLog (duration, disposition)
        throw new NotImplementedException("Implemented in Step 4");
    }
}
