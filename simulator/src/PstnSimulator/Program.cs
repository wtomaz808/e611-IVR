using PstnSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ───────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();

// Simulator core services
builder.Services.AddSingleton<CallStateManager>();
builder.Services.AddSingleton<PstnService>();
builder.Services.AddSingleton<Cm10Service>();

// ACS / Teams Bridge — selected by AcsMode configuration:
//   Mock       → Legacy ACS mock bridge (EventGrid + ACS callback format)
//   TeamsBot   → Teams calling bot mock bridge (commsNotifications + Graph API)
//   Live       → Real ACS (LiveAcsBridge)
var acsMode = builder.Configuration["AcsMode"] ?? "Mock";

if (acsMode.Equals("Live", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAcsBridge, LiveAcsBridge>();
}
else if (acsMode.Equals("TeamsBot", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAcsBridge, MockTeamsBridge>();
}
else
{
    builder.Services.AddSingleton<IAcsBridge, MockAcsBridge>();
}

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// ─── Mock ACS Call Automation REST API (legacy mode) ────────
// Active when AcsMode=Mock. Mimics ACS REST API so IVR Functions
// can use: AcsConnectionString=endpoint=http://pstn-simulator:8080;accesskey=bW9ja2tleQ==
if (acsMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
{
    var mockBridge = app.Services.GetRequiredService<IAcsBridge>() as MockAcsBridge;
    if (mockBridge != null)
    {
        app.Map("/calling/{**path}", async (HttpContext ctx, string path) =>
        {
            return await mockBridge.HandleAcsApiAsync(ctx, path);
        });
    }
}

// ─── Mock Microsoft Graph Calling API (Teams bot mode) ──────
// Active when AcsMode=TeamsBot.
// The IVR Functions are configured with:
//   GraphApiEndpoint = http://pstn-simulator:8080/graph/v1.0
//   MicrosoftAppId   = simulator (no real auth needed in local dev)
// Intercepts Graph call control commands and fires follow-up
// commsNotifications back to the bot endpoint.
if (acsMode.Equals("TeamsBot", StringComparison.OrdinalIgnoreCase))
{
    var teamsBridge = app.Services.GetRequiredService<IAcsBridge>() as MockTeamsBridge;
    if (teamsBridge != null)
    {
        // Token endpoint — return a mock token so GraphServiceClient doesn't fail auth
        app.MapPost("/graph/v1.0/oauth2/token", () => Results.Ok(new
        {
            access_token = "mock-simulator-token",
            token_type   = "Bearer",
            expires_in   = 3600
        }));
        app.MapPost("/graph/v1.0/common/oauth2/v2.0/token", () => Results.Ok(new
        {
            access_token = "mock-simulator-token",
            token_type   = "Bearer",
            expires_in   = 3600
        }));

        // All Graph Calling API routes: /graph/v1.0/communications/calls/...
        app.Map("/graph/v1.0/{**path}", async (HttpContext ctx, string path) =>
        {
            return await teamsBridge.HandleGraphApiAsync(ctx, path);
        });
    }
}

// ─── Mock External API ─────────────────────────────────────
app.MapPost("/api/external/work-order", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();

    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("ExternalApi");

    logger.LogInformation("═══════════════════════════════════════════════════");
    logger.LogInformation("  EXTERNAL API — Work Order Received");
    logger.LogInformation("═══════════════════════════════════════════════════");
    logger.LogInformation("{Body}", body);
    logger.LogInformation("═══════════════════════════════════════════════════");

    var workOrderId = $"WO-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
    return Results.Ok(new
    {
        success = true,
        workOrderId,
        message = $"Work order {workOrderId} created successfully",
        receivedAt = DateTime.UtcNow
    });
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
