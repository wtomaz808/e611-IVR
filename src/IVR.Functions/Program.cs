using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using Azure.Identity;
using Azure.Storage.Blobs;
using IVR.Core.Interfaces;
using IVR.Core.Services;
using IVR.Functions.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // ─── Azure Communication Services ───────────────────────
        var acsConnectionString = context.Configuration["AcsConnectionString"];
        if (!string.IsNullOrEmpty(acsConnectionString))
        {
            services.AddSingleton(new CallAutomationClient(acsConnectionString));
        }
        else
        {
            // Mock mode — point ACS SDK at the PSTN Simulator's mock REST API
            var mockAcsEndpoint = context.Configuration["MockAcsEndpoint"] ?? "http://pstn-simulator:8080";
            var mockConnStr = $"endpoint={mockAcsEndpoint};accesskey=bW9ja2tleQ==";
            services.AddSingleton(new CallAutomationClient(mockConnStr));
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
        services.AddSingleton<TranscriptRoutingService>();

        // HTTP client factory for external system integrations
        services.AddHttpClient("ExternalSystems", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
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
