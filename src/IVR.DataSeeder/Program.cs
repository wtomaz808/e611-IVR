using IVR.Core.Models;
using IVR.Core.Services;
using IVR.DataSeeder;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   E911 IVR System - Test Data Seeder                    ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var connectionString = configuration["CosmosDb:ConnectionString"];
var databaseName = configuration["CosmosDb:DatabaseName"] ?? "IvrDatabase";
var webhookBaseUrl = configuration["ExternalSystems:WebhookBaseUrl"] ?? "http://pstn-simulator:8080";

// Prefer Key Vault over a plaintext connection string in config.
var keyVaultUri = configuration["KeyVault:Uri"];
if (string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(keyVaultUri))
{
    Console.WriteLine($"🔐 Fetching Cosmos DB connection string from Key Vault ({keyVaultUri})...");
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        // Azure Government vaults use a different AAD authority than public cloud.
        AuthorityHost = keyVaultUri.Contains("usgovcloudapi.net", StringComparison.OrdinalIgnoreCase)
            ? AzureAuthorityHosts.AzureGovernment
            : AzureAuthorityHosts.AzurePublicCloud
    });
    var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
    var secretName = configuration["KeyVault:CosmosDbSecretName"] ?? "CosmosDbConnectionString";
    var secret = await secretClient.GetSecretAsync(secretName);
    connectionString = secret.Value.Value;
    Console.WriteLine("✅ Retrieved connection string from Key Vault");
    Console.WriteLine();
}

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ Error: CosmosDb:ConnectionString not configured.");
    Console.WriteLine();
    Console.WriteLine("Please set the connection string in one of these ways:");
    Console.WriteLine("  1. Set KeyVault:Uri in appsettings.json to fetch it from Key Vault (recommended)");
    Console.WriteLine("  2. Add to appsettings.json -> CosmosDb:ConnectionString");
    Console.WriteLine("  3. Set environment variable: CosmosDb__ConnectionString");
    Console.WriteLine("  4. Pass as command line: dotnet run --CosmosDb:ConnectionString=\"...\"");
    Console.WriteLine();
    return 1;
}


try
{
    Console.WriteLine($"📦 Connecting to Cosmos DB...");
    Console.WriteLine($"   Database: {databaseName}");
    Console.WriteLine();

    // Create Cosmos Client with same serialization options as AdminPortal
    using var cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
    
    // Create logger factory
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<CosmosDbService>();
    
    // Use CosmosDbService instead of raw SDK
    var cosmosService = new CosmosDbService(cosmosClient, logger, databaseName);

    // Test access
    Console.WriteLine($"🔍 Testing container access...");
    var existingPrompts = await cosmosService.GetAllPromptsAsync();
    Console.WriteLine($"  ✅ Prompts container accessible ({existingPrompts.Count} existing)");
    Console.WriteLine();

    // Track overall statistics
    int totalSuccess = 0;
    int totalErrors = 0;

    // ========================================
    // SEED PROMPTS
    // ========================================
    var prompts = PromptSeeder.GenerateTestPrompts();
    Console.WriteLine($"🎙️  SEEDING PROMPTS ({prompts.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int promptSuccess = 0;
    int promptErrors = 0;

    foreach (var prompt in prompts)
    {
        try
        {
            await cosmosService.UpsertPromptAsync(prompt);
            Console.WriteLine($"  ✅ {prompt.Name} ({prompt.Category})");
            promptSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {prompt.Name}: {ex.Message}");
            promptErrors++;
        }
    }

    Console.WriteLine($"  📊 Prompts: {promptSuccess} succeeded, {promptErrors} failed");
    Console.WriteLine();

    totalSuccess += promptSuccess;
    totalErrors += promptErrors;

    // ========================================
    // SEED ANI RECORDS
    // ========================================
    var aniRecords = AniAliSeeder.GenerateTestAniRecords();
    Console.WriteLine($"📞 SEEDING ANI RECORDS ({aniRecords.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int aniSuccess = 0;
    int aniErrors = 0;

    foreach (var ani in aniRecords)
    {
        try
        {
            await cosmosService.UpsertAniRecordAsync(ani);
            Console.WriteLine($"  ✅ {ani.PhoneNumber} - {ani.CallerName} ({ani.CallerType})");
            aniSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {ani.PhoneNumber}: {ex.Message}");
            aniErrors++;
        }
    }

    Console.WriteLine($"  📊 ANI Records: {aniSuccess} succeeded, {aniErrors} failed");
    Console.WriteLine();

    totalSuccess += aniSuccess;
    totalErrors += aniErrors;

    // ========================================
    // SEED ALI RECORDS
    // ========================================
    var aliRecords = AniAliSeeder.GenerateTestAliRecords();
    Console.WriteLine($"📍 SEEDING ALI RECORDS ({aliRecords.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int aliSuccess = 0;
    int aliErrors = 0;

    foreach (var ali in aliRecords)
    {
        try
        {
            await cosmosService.UpsertAliRecordAsync(ali);
            var location = $"{ali.Address.City}, {ali.Address.State}";
            Console.WriteLine($"  ✅ {ali.PhoneNumber} - {location} ({ali.LocationType})");
            aliSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {ali.PhoneNumber}: {ex.Message}");
            aliErrors++;
        }
    }

    Console.WriteLine($"  📊 ALI Records: {aliSuccess} succeeded, {aliErrors} failed");
    Console.WriteLine();

    totalSuccess += aliSuccess;
    totalErrors += aliErrors;

    // ========================================
    // SEED MENUS (Call Flows)
    // ========================================
    var menus = MenuSeeder.GenerateTestMenus(webhookBaseUrl);
    Console.WriteLine($"📋 SEEDING MENUS / CALL FLOWS ({menus.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int menuSuccess = 0;
    int menuErrors = 0;

    foreach (var menu in menus)
    {
        try
        {
            await cosmosService.UpsertMenuAsync(menu);
            Console.WriteLine($"  ✅ {menu.Name} ({menu.MenuType})");
            menuSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {menu.Name}: {ex.Message}");
            menuErrors++;
        }
    }

    Console.WriteLine($"  📊 Menus: {menuSuccess} succeeded, {menuErrors} failed");
    Console.WriteLine();

    totalSuccess += menuSuccess;
    totalErrors += menuErrors;

    // ========================================
    // SEED TEAM ROUTING
    // ========================================
    var teamConfigs = TeamRoutingSeeder.GenerateTestTeamRoutingConfigs();
    Console.WriteLine($"👥 SEEDING TEAM ROUTING ({teamConfigs.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int teamSuccess = 0;
    int teamErrors = 0;

    foreach (var team in teamConfigs)
    {
        try
        {
            await cosmosService.UpsertTeamRoutingConfigAsync(team);
            Console.WriteLine($"  ✅ {team.TeamName} (Priority: {team.Priority})");
            teamSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {team.TeamName}: {ex.Message}");
            teamErrors++;
        }
    }

    Console.WriteLine($"  📊 Team Routing: {teamSuccess} succeeded, {teamErrors} failed");
    Console.WriteLine();

    totalSuccess += teamSuccess;
    totalErrors += teamErrors;

    // ========================================
    // SEED EXTERNAL SYSTEMS
    // ========================================
    var externalSystems = ExternalSystemSeeder.GenerateTestExternalSystems(webhookBaseUrl);
    Console.WriteLine($"🔌 SEEDING EXTERNAL SYSTEMS ({externalSystems.Count} total)");
    Console.WriteLine("────────────────────────────────────────────");

    int externalSystemSuccess = 0;
    int externalSystemErrors = 0;

    foreach (var system in externalSystems)
    {
        try
        {
            await cosmosService.UpsertExternalSystemConfigAsync(system);
            Console.WriteLine($"  ✅ {system.SystemName} ({system.BaseUrl})");
            externalSystemSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {system.SystemName}: {ex.Message}");
            externalSystemErrors++;
        }
    }

    Console.WriteLine($"  📊 External Systems: {externalSystemSuccess} succeeded, {externalSystemErrors} failed");
    Console.WriteLine();

    totalSuccess += externalSystemSuccess;
    totalErrors += externalSystemErrors;

    // ========================================
    // SEED DATA EXTRACTION CONFIGS
    // ========================================
    var dataExtractionConfigs = ExternalSystemSeeder.GenerateTestDataExtractionConfigs();
    Console.WriteLine($"🧠 SEEDING DATA EXTRACTION CONFIGS ({dataExtractionConfigs.Count} total)");
    Console.WriteLine("────────────────────────────────────────────");

    int dataExtractionSuccess = 0;
    int dataExtractionErrors = 0;

    foreach (var extraction in dataExtractionConfigs)
    {
        try
        {
            await cosmosService.UpsertDataExtractionConfigAsync(extraction);
            Console.WriteLine($"  ✅ {extraction.Name}");
            dataExtractionSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {extraction.Name}: {ex.Message}");
            dataExtractionErrors++;
        }
    }

    Console.WriteLine($"  📊 Data Extraction Configs: {dataExtractionSuccess} succeeded, {dataExtractionErrors} failed");
    Console.WriteLine();

    totalSuccess += dataExtractionSuccess;
    totalErrors += dataExtractionErrors;

    // ========================================
    // SEED PHONE NUMBER CONFIGS (Teams DIDs)
    // ========================================
    var phoneNumbers = PhoneNumberSeeder.GeneratePhoneNumberConfigs();
    Console.WriteLine($"📱 SEEDING PHONE NUMBER CONFIGS ({phoneNumbers.Count} total)");
    Console.WriteLine("─────────────────────────────────────────────────────────");

    int phoneSuccess = 0;
    int phoneErrors = 0;

    foreach (var pn in phoneNumbers)
    {
        try
        {
            await cosmosService.UpsertPhoneNumberConfigAsync(pn);
            Console.WriteLine($"  ✅ {pn.PhoneNumber} - {pn.Label} ({pn.NumberType})");
            phoneSuccess++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {pn.PhoneNumber}: {ex.Message}");
            phoneErrors++;
        }
    }

    Console.WriteLine($"  📊 Phone Numbers: {phoneSuccess} succeeded, {phoneErrors} failed");
    Console.WriteLine();

    totalSuccess += phoneSuccess;
    totalErrors += phoneErrors;

    // ========================================
    // SUMMARY
    // ========================================
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine($"✨ SEEDING COMPLETE!");
    Console.WriteLine($"   Total Success: {totalSuccess}");
    if (totalErrors > 0)
    {
        Console.WriteLine($"   Total Errors: {totalErrors}");
    }
    Console.WriteLine("─────────────────────────────────────────────────────────");
    Console.WriteLine($"   Prompts:        {promptSuccess}/{prompts.Count}");
    Console.WriteLine($"   ANI Records:    {aniSuccess}/{aniRecords.Count}");
    Console.WriteLine($"   ALI Records:    {aliSuccess}/{aliRecords.Count}");
    Console.WriteLine($"   Menus:          {menuSuccess}/{menus.Count}");
    Console.WriteLine($"   Team Routing:   {teamSuccess}/{teamConfigs.Count}");
    Console.WriteLine($"   External Systems:      {externalSystemSuccess}/{externalSystems.Count}");
    Console.WriteLine($"   Data Extraction Configs: {dataExtractionSuccess}/{dataExtractionConfigs.Count}");
    Console.WriteLine($"   Phone Numbers:  {phoneSuccess}/{phoneNumbers.Count}");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("📋 Next steps:");
    Console.WriteLine("  1. Admin Portal → Prompts - View and test prompts");
    Console.WriteLine("  2. Admin Portal → ANI/ALI - Verify caller/location data");
    Console.WriteLine("  3. Admin Portal → Call Flows - Review menu structure");
    Console.WriteLine("  4. Admin Portal → Team Routing - Verify routing configs");
    Console.WriteLine("  5. Admin Portal → Settings - Configure business hours");
    Console.WriteLine("  3. Admin Portal → Call Flows - Design call routing");
    Console.WriteLine("  4. Admin Portal → Settings - Configure business hours");
    Console.WriteLine();

    return totalErrors > 0 ? 1 : 0;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Fatal error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}
