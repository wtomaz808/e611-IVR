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

// ACS Bridge — Mock or Live based on configuration
var acsMode = builder.Configuration["AcsMode"] ?? "Mock";
if (acsMode.Equals("Live", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAcsBridge, LiveAcsBridge>();
}
else
{
    builder.Services.AddSingleton<IAcsBridge, MockAcsBridge>();
}

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// ─── Mock ACS Call Automation REST API ──────────────────────
// These endpoints mimic the ACS REST API so the IVR Functions
// can connect to the simulator instead of real Azure.
// The IVR sets: ACS_CONNECTION_STRING=endpoint=http://pstn-simulator:8080;accesskey=bW9ja2tleQ==
if (acsMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
{
    // Resolve the mock bridge once at startup
    var mockBridge = app.Services.GetRequiredService<IAcsBridge>() as MockAcsBridge;
    if (mockBridge != null)
    {
        // Catch-all route for /calling/* paths (ACS uses colons in paths like :answer, :play)
        app.Map("/calling/{**path}", async (HttpContext ctx, string path) =>
        {
            return await mockBridge.HandleAcsApiAsync(ctx, path);
        });
    }
}

// ─── Mock External API ─────────────────────────────────────
// Simulates the external client system that receives IVR-collected data
// (property, inspection type, caller role, etc.) for work order creation.
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

    // Return a mock work order confirmation
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
