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

// ─── Azure Bot Service (Teams Calling) ─────────────────────────
// The functionApp module is deployed first so we can pass its
// hostname to the bot service as the messaging endpoint.
// Bicep resolves this via the dependsOn implicit reference.
module botService 'modules/bot-service.bicep' = {
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
// gpt-4.1 (2025-04-14) — confirmed available in Azure Government.
// gpt-4.5 is NOT yet available in Azure Government as of July 2026.
// Re-evaluate when https://aka.ms/oai/gov-models is updated.
module openAI 'modules/openai.bicep' = {
  params: {
    name: '${namePrefix}-openai-${uniqueSuffix}'
    location: location
    tags: tags
    deploymentName: 'gpt-41'
    modelName: 'gpt-4.1'
    modelVersion: '2025-04-14'
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
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    botAppId: teamsBotAppId
    botAppPassword: teamsBotAppPassword
    botTenantId: azureAdTenantId
    cognitiveServicesEndpoint: cognitiveServices.outputs.endpoint
    cognitiveServicesKey: cognitiveServices.outputs.primaryKey
    openAIEndpoint: openAI.outputs.endpoint
    openAIKey: openAI.outputs.primaryKey
    openAIDeploymentName: openAI.outputs.deploymentName
  }
}

// ─── App Service (Admin Portal) ─────────────────────────────────
module appService 'modules/app-service.bicep' = {
  params: {
    name: '${namePrefix}-admin-${uniqueSuffix}'
    location: location
    tags: tags
    cosmosDbConnectionString: cosmosDb.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    appInsightsConnectionString: appInsights.outputs.connectionString
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
  }
}

// ─── App Service (PSTN Simulator) ───────────────────────────────
module simulatorApp 'modules/simulator-app.bicep' = {
  params: {
    name: '${namePrefix}-simulator-${uniqueSuffix}'
    location: location
    tags: tags
    appServicePlanId: appService.outputs.planId
    ivrEndpoint: 'https://${functionApp.outputs.defaultHostname}'
    acsMode: 'Mock'  // Use Mock mode by default for testing
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
output botServiceName string = botService.outputs.botName

@description('Bot messaging endpoint — configure this in Teams Admin Center calling webhook')
output botMessagingEndpoint string = botService.outputs.messagingEndpoint
