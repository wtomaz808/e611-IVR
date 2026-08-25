using IVR.Core.Interfaces;
using IVR.Core.Services;
using IVR.McpServer.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<FacilityTools>()
    .WithTools<EventScheduleTools>()
    .WithTools<CallHistoryTools>()
    .WithTools<CallEventTools>();

builder.Services.AddSingleton<IEventScheduleEvaluationService, EventScheduleEvaluationService>();

// ─── Cosmos DB (falls back to in-memory seed data, matching Function App / Admin Portal) ───
var cosmosConnectionString = builder.Configuration["CosmosDbConnectionString"];
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    }));
    builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
}
else
{
    builder.Services.AddSingleton<ICosmosDbService, SeededInMemoryCosmosDbService>();
}

// ─── Entra authentication for the MCP endpoint ───────────────────────────
// Every live-call tool invocation must come from an authorized client
// (the IVR Function App's managed identity). Disable only for local/dev
// use (e.g. docker-compose, no Entra app registration available).
var requireAuth = builder.Configuration.GetValue("Mcp:RequireAuthentication", true);
var tenantId = builder.Configuration["Mcp:TenantId"];
var audience = builder.Configuration["Mcp:Audience"];

if (requireAuth)
{
    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(audience))
    {
        throw new InvalidOperationException(
            "Mcp:RequireAuthentication is true but Mcp:TenantId / Mcp:Audience are not configured. " +
            "Set both, or explicitly set Mcp:RequireAuthentication=false for local/dev-only use.");
    }

    // Azure Government uses a separate authority host than commercial Azure.
    var isGovCloud = tenantId.Contains("usgov", StringComparison.OrdinalIgnoreCase)
                      || (builder.Configuration["Mcp:Authority"]?.Contains("microsoftonline.us", StringComparison.OrdinalIgnoreCase) ?? false);
    var authority = builder.Configuration["Mcp:Authority"]
                     ?? (isGovCloud
                         ? $"https://login.microsoftonline.us/{tenantId}/v2.0"
                         : $"https://login.microsoftonline.com/{tenantId}/v2.0");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true
            };
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

if (requireAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapMcp("/mcp").RequireAuthorization();
}
else
{
    app.MapMcp("/mcp");
}

app.Run();
