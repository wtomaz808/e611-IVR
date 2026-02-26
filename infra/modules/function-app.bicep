// ─────────────────────────────────────────────────────────────────
// Azure Function App — IVR Call Handling Engine
// ─────────────────────────────────────────────────────────────────

@description('Function App name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Storage account name for Functions runtime')
param storageAccountName string

@secure()
@description('Storage account key')
param storageAccountKey string

@description('App Insights connection string')
param appInsightsConnectionString string

@secure()
@description('Cosmos DB connection string')
param cosmosDbConnectionString string

@secure()
@description('Azure Communication Services connection string')
param acsConnectionString string

@description('Cognitive Services endpoint')
param cognitiveServicesEndpoint string

@secure()
@description('Cognitive Services key')
param cognitiveServicesKey string

@description('Azure OpenAI endpoint')
param openAIEndpoint string

@secure()
@description('Azure OpenAI key')
param openAIKey string

@description('Azure OpenAI deployment name')
param openAIDeploymentName string

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Linux
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'CosmosDbConnectionString', value: cosmosDbConnectionString }
        { name: 'AcsConnectionString', value: acsConnectionString }
        { name: 'StorageConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'CognitiveServicesEndpoint', value: cognitiveServicesEndpoint }
        { name: 'CognitiveServicesKey', value: cognitiveServicesKey }
        { name: 'AzureOpenAI__Endpoint', value: openAIEndpoint }
        { name: 'AzureOpenAI__ApiKey', value: openAIKey }
        { name: 'AzureOpenAI__DeploymentName', value: openAIDeploymentName }
        { name: 'CallbackBaseUrl', value: 'https://${name}.azurewebsites.net' }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

@description('Function App default hostname')
output defaultHostname string = 'https://${functionApp.properties.defaultHostName}'

@description('Function App resource ID')
output resourceId string = functionApp.id
