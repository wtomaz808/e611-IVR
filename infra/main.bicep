// ─────────────────────────────────────────────────────────────────
// IVR System — Main Bicep Deployment
// Deploys all Azure resources for the IVR system
// ─────────────────────────────────────────────────────────────────

targetScope = 'resourceGroup'

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name prefix for all resources')
param baseName string = 'ivr'

@description('Admin portal Azure AD tenant ID')
@secure()
param azureAdTenantId string

@description('Admin portal Azure AD client ID')
@secure()
param azureAdClientId string

@description('Teams Calling Bot — Entra App Registration client ID. Run scripts/Create-BotAppRegistration.ps1 to generate this value.')
param teamsBotAppId string

@description('Teams Calling Bot — Entra App Registration client secret')
@secure()
param teamsBotAppPassword string

@description('Deploy the Azure Bot Service resource via Bicep. Set false for Azure Government subscriptions where the ARM provider returns APS errors; create the bot manually in the portal instead.')
param deployBotService bool = false

@description('Override the Graph API endpoint used by the IVR Functions. Set to the PSTN simulator URL for simulator-based testing, e.g. https://<simulator-host>/graph/v1.0. Empty = use cloud default.')
param simulatorGraphEndpoint string = ''

// ─── Naming Convention ──────────────────────────────────────────
var uniqueSuffix = uniqueString(resourceGroup().id)
var namePrefix = '${baseName}-${environmentName}'

// ─── Cosmos DB ──────────────────────────────────────────────────
module cosmosDb 'modules/cosmos-db.bicep' = {
  params: {
    name: '${namePrefix}-cosmos-${uniqueSuffix}'
    location: location
    tags: tags
  }
}

// ─── Storage Account (Prompts + Functions) ──────────────────────
module storage 'modules/storage.bicep' = {
  params: {
    name: '${baseName}${environmentName}stor${uniqueSuffix}'
    location: location
    tags: tags
  }
}

// ─── Key Vault (secrets referenced by App Settings) ─────────────
// Named '${baseName}-kv-${uniqueSuffix}' (not namePrefix) to stay within
// Key Vault's 24-character name limit.
module keyVault 'modules/keyvault.bicep' = {
  params: {
    name: '${baseName}-kv-${uniqueSuffix}'
    location: location
    tags: tags
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
  }
}

// ─── Azure Bot Service (Teams Calling) ─────────────────────────
// The functionApp module is deployed first so we can pass its
// hostname to the bot service as the messaging endpoint.
// Bicep resolves this via the dependsOn implicit reference.
// NOTE: Set deployBotService=false for Azure Government subscriptions
// that return 'APS not implemented' — create the bot manually in the portal.
module botService 'modules/bot-service.bicep' = if (deployBotService) {
  params: {
    name: '${namePrefix}-bot-${uniqueSuffix}'
    location: location
    tags: tags
    microsoftAppId: teamsBotAppId
    microsoftAppTenantId: azureAdTenantId
    messagingEndpoint: 'https://${functionApp.outputs.rawHostname}/api/bot-messages'
  }
}

// ─── Cognitive Services (Speech) ────────────────────────────────
module cognitiveServices 'modules/cognitive-services.bicep' = {
  params: {
    name: '${namePrefix}-speech-${uniqueSuffix}'
    location: location
    tags: tags
  }
}

// ─── Azure OpenAI (Transcript Intent Classification) ────────────
// gpt-5.1 (2025-11-13) is registered in usgovvirginia but has no SKUs
// configured — cannot be deployed via API. Using gpt-4.1 (2025-04-14)
// which is confirmed available. Re-check gpt-5.1 availability with your
// MSFT team (may require provisioned throughput or special approval).
module openAI 'modules/openai.bicep' = {
  params: {
    name: '${namePrefix}-openai-${uniqueSuffix}'
    location: location
    tags: tags
    deploymentName: 'gpt-41'
    modelName: 'gpt-4.1'
    modelVersion: '2025-04-14'
    deploymentSku: 'Standard'
  }
}

// ─── Application Insights ───────────────────────────────────────
module appInsights 'modules/app-insights.bicep' = {
  params: {
    name: '${namePrefix}-insights-${uniqueSuffix}'
    location: location
    tags: tags
  }
}

// ─── Function App (IVR Call Handling) ───────────────────────────
module functionApp 'modules/function-app.bicep' = {
  params: {
    name: '${namePrefix}-func-${uniqueSuffix}'
    location: location
    tags: tags
    storageAccountName: storage.outputs.name
    storageAccountKey: storage.outputs.primaryKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    cosmosDbConnectionString: keyVault.outputs.cosmosDbConnectionStringRef
    botAppId: teamsBotAppId
    botAppPassword: teamsBotAppPassword
    botTenantId: azureAdTenantId
    cognitiveServicesEndpoint: cognitiveServices.outputs.endpoint
    cognitiveServicesKey: cognitiveServices.outputs.primaryKey
    openAIEndpoint: openAI.outputs.endpoint
    openAIKey: openAI.outputs.primaryKey
    openAIDeploymentName: openAI.outputs.deploymentName
    graphApiEndpointOverride: simulatorGraphEndpoint
  }
}

// ─── App Service (Admin Portal) ─────────────────────────────────
module appService 'modules/app-service.bicep' = {
  params: {
    name: '${namePrefix}-admin-${uniqueSuffix}'
    location: location
    tags: tags
    cosmosDbConnectionString: keyVault.outputs.cosmosDbConnectionStringRef
    storageConnectionString: storage.outputs.connectionString
    appInsightsConnectionString: appInsights.outputs.connectionString
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
  }
}

// ─── Key Vault access — grant both apps' managed identities read access ──
module keyVaultAccess 'modules/keyvault-access.bicep' = {
  params: {
    keyVaultName: keyVault.outputs.name
    principalIds: [
      functionApp.outputs.principalId
      appService.outputs.principalId
    ]
  }
}

// ─── App Service (PSTN Simulator) ───────────────────────────────
module simulatorApp 'modules/simulator-app.bicep' = {
  params: {
    name: '${namePrefix}-simulator-${uniqueSuffix}'
    location: location
    tags: tags
    appServicePlanId: appService.outputs.planId
    ivrEndpoint: functionApp.outputs.defaultHostname
    acsMode: 'TeamsBot'  // Teams calling bot mode — no ACS
  }
}

// ─── Tags ───────────────────────────────────────────────────────
var tags = {
  environment: environmentName
  project: 'ivr-system'
  managedBy: 'bicep'
}

// ─── Outputs ────────────────────────────────────────────────────
@description('Function App URL for callback configuration')
output functionAppUrl string = functionApp.outputs.defaultHostname

@description('Admin Portal URL')
output adminPortalUrl string = appService.outputs.defaultHostname

@description('PSTN Simulator URL')
output simulatorUrl string = simulatorApp.outputs.defaultHostname

@description('Bot Service name — register this in Teams Admin Center')
output botServiceName string = deployBotService ? (botService.outputs.botName ?? 'unknown') : 'create-manually-in-portal'

@description('Bot messaging endpoint — configure this in Teams Admin Center calling webhook')
output botMessagingEndpoint string = 'https://${functionApp.outputs.rawHostname}/api/bot-messages'
