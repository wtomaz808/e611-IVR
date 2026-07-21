using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using IVR.Core.Interfaces;
using IVR.Core.Services;
using IVR.Functions.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // ─── Bot Framework — Teams Calling adapter ───────────────
        // CloudAdapter reads the following from IConfiguration
        // (all set via Function App settings in infra/modules/function-app.bicep):
        //   MicrosoftAppType     = SingleTenant
        //   MicrosoftAppId       = Entra App Registration client ID
        //   MicrosoftAppPassword = Entra App Registration client secret
        //   MicrosoftAppTenantId = Entra tenant ID
        //   ChannelService       = https://botframework.azure.us  (GCC High)
        //                          (empty = commercial Azure)
        // The adapter validates every inbound Teams request JWT before
        // dispatching to TeamsCallBot.
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
        services.AddSingleton<IBotFrameworkHttpAdapter, CloudAdapter>();

        // ─── Microsoft Graph — call control ─────────────────────
        // GraphServiceClient issues the commands that actually control
        // the Teams call: answer, playPrompt, recordResponse, transfer,
        // and delete (hang up).
        //
        // Auth uses ClientSecretCredential (app-only, no user sign-in).
        // Authority host is automatically selected for GCC High vs commercial.
        var botAppId       = context.Configuration["MicrosoftAppId"];
        var botAppPassword = context.Configuration["MicrosoftAppPassword"];
        var botTenantId    = context.Configuration["MicrosoftAppTenantId"];
        var graphEndpoint  = context.Configuration["GraphApiEndpoint"]
                             ?? "https://graph.microsoft.com/v1.0";

        // Gov cloud is determined by ChannelService (reliable) rather than
        // GraphApiEndpoint, which may point to a simulator URL in dev/demo mode.
        var channelService = context.Configuration["ChannelService"] ?? "";
        var isGovCloud = channelService.Contains("azure.us", StringComparison.OrdinalIgnoreCase)
                      || graphEndpoint.Contains("microsoft.us", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(botAppId) && !string.IsNullOrEmpty(botAppPassword)
                                             && !string.IsNullOrEmpty(botTenantId))
        {
            var authorityHost = isGovCloud
                ? AzureAuthorityHosts.AzureGovernment
                : AzureAuthorityHosts.AzurePublicCloud;

            var credential = new ClientSecretCredential(
                botTenantId, botAppId, botAppPassword,
                new ClientSecretCredentialOptions { AuthorityHost = authorityHost });

            services.AddSingleton(new GraphServiceClient(credential, baseUrl: graphEndpoint));
        }
        else
        {
            // Local / CI mode without bot credentials configured.
            // TeamsCallBot checks for null and skips Graph calls with a warning.
            services.AddSingleton<GraphServiceClient>(_ =>
            {
                var logger = _.GetRequiredService<ILogger<GraphServiceClient>>();
                logger.LogWarning(
                    "GraphServiceClient not configured — MicrosoftAppId/Password/TenantId missing. " +
                    "Teams call control will be disabled. Set these values for production.");
                return null!;
            });
        }

        // ─── Cosmos DB ──────────────────────────────────────────
        var cosmosConnectionString = context.Configuration["CosmosDbConnectionString"];
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            services.AddSingleton(new CosmosClient(cosmosConnectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }));
            services.AddSingleton<ICosmosDbService, CosmosDbService>();
        }
        else
        {
            // No Cosmos DB — use in-memory store with seed data
            services.AddSingleton<ICosmosDbService, SeededInMemoryCosmosDbService>();
        }

        // ─── Blob Storage ───────────────────────────────────────
        var storageConnectionString = context.Configuration["StorageConnectionString"];
        if (!string.IsNullOrEmpty(storageConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(storageConnectionString));
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }
        else
        {
            // No Blob Storage — use no-op (prompts use TTS text only)
            services.AddSingleton<IBlobStorageService, NoOpBlobStorageService>();
        }

        // ─── Azure OpenAI (optional — needed for speech routing) ─
        var openAIEndpoint = context.Configuration["AzureOpenAI:Endpoint"];
        var openAIKey = context.Configuration["AzureOpenAI:ApiKey"];

        if (!string.IsNullOrEmpty(openAIEndpoint))
        {
            if (!string.IsNullOrEmpty(openAIKey))
            {
                services.AddSingleton(new AzureOpenAIClient(
                    new Uri(openAIEndpoint),
                    new System.ClientModel.ApiKeyCredential(openAIKey)));
            }
            else
            {
                services.AddSingleton(new AzureOpenAIClient(
                    new Uri(openAIEndpoint),
                    new DefaultAzureCredential()));
            }
        }
        else
        {
            // No OpenAI — register a null placeholder; speech routing will be disabled
            services.AddSingleton<AzureOpenAIClient>(sp =>
            {
                // Dummy client — TranscriptRoutingService will catch exceptions
                return new AzureOpenAIClient(
                    new Uri("https://not-configured.openai.azure.com"),
                    new System.ClientModel.ApiKeyCredential("not-configured"));
            });
        }

        // Services
        services.AddSingleton<ICallFlowEngine, CallFlowEngine>();
        services.AddSingleton<AniAliService>();
        services.AddSingleton<PromptService>();
        services.AddSingleton<TtsGenerationService>();
        services.AddSingleton<TranscriptRoutingService>();

        // HTTP client factory for external system integrations
        services.AddHttpClient("ExternalSystems", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        // HTTP client for TTS audio generation via Cognitive Services REST API
        services.AddHttpClient("TtsService", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<ExternalSystemIntegrationService>();

        services.AddMemoryCache();
    })
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

host.Run();
