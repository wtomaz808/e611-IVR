using Azure.Storage.Blobs;
using IVR.AdminPortal.Services;
using IVR.Core.Interfaces;
using IVR.Core.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ─── Authentication (Azure AD) ──────────────────────────────────
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrEmpty(azureAdClientId))
{
    builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
    builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("IVR.Admin"));
    });
}
else
{
    // No Azure AD configured — allow anonymous access (dev/local mode)
    builder.Services.AddControllersWithViews();
    builder.Services.AddAuthorization();
}

// ─── Blazor Services ────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ─── Azure Services ─────────────────────────────────────────────
var cosmosConnectionString = builder.Configuration["CosmosDbConnectionString"];
var storageConnectionString = builder.Configuration["StorageConnectionString"];

if (!string.IsNullOrEmpty(cosmosConnectionString) && cosmosConnectionString.Contains("AccountEndpoint"))
{
    builder.Services.AddSingleton(sp =>
    {
        return new CosmosClient(cosmosConnectionString, new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
    });
    builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
}
else
{
    // No Cosmos DB configured — use in-memory store with seed data
    builder.Services.AddSingleton<ICosmosDbService, SeededInMemoryCosmosDbService>();
}

if (!string.IsNullOrEmpty(storageConnectionString) && storageConnectionString != "<your-storage-connection-string>")
{
    builder.Services.AddSingleton(sp => new BlobServiceClient(storageConnectionString));
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, NoOpBlobStorageService>();
}

// ─── Application Services ───────────────────────────────────────
builder.Services.AddScoped<AdminDashboardService>();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
