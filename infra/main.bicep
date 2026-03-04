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

// ─── Communication Services ────────────────────────────────────
module communicationServices 'modules/communication-services.bicep' = {
  params: {
    name: '${namePrefix}-acs-${uniqueSuffix}'
    location: 'global' // ACS is a global resource
    tags: tags
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
// Using gpt-4.1 model (version 2025-04-14) available in Azure Government
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
    acsConnectionString: communicationServices.outputs.connectionString
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

@description('Communication Services resource ID')
output acsResourceId string = communicationServices.outputs.resourceId
