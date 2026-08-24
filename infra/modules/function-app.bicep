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

@description('Entra App Registration client ID for the Teams Calling Bot')
param botAppId string

@secure()
@description('Entra App Registration client secret for the Teams Calling Bot')
param botAppPassword string

@description('Entra tenant ID that owns the bot App Registration')
param botTenantId string

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

@description('Override the Graph API endpoint. Set to the PSTN simulator URL for simulator testing (e.g. https://my-simulator.azurewebsites.us/graph/v1.0). Empty string uses the cloud-default endpoint.')
param graphApiEndpointOverride string = ''

@description('MCP server endpoint (Streamable HTTP base URL). Empty disables MCP-backed lookups.')
param mcpEndpoint string = ''

@description('Entra audience to request when acquiring a token for the MCP server')
param mcpAudience string = ''

@description('Enable MCP-backed lookups in the Function App. Requires Phase 3 client integration code.')
param mcpEnabled bool = false

// ─── Cloud-specific Bot Framework endpoints ─────────────────────
// Azure Government (GCC High) uses separate auth and channel service
// endpoints. These are automatically selected based on the deployment
// environment.
var isGovCloud = environment().name == 'AzureUSGovernment'
var channelService = isGovCloud ? 'https://botframework.azure.us' : ''
var oAuthUrl = isGovCloud ? 'https://login.microsoftonline.us' : environment().authentication.loginEndpoint
var defaultGraphApiEndpoint = isGovCloud ? 'https://graph.microsoft.us/v1.0' : 'https://graph.microsoft.com/v1.0'
var graphApiEndpoint = !empty(graphApiEndpointOverride) ? graphApiEndpointOverride : defaultGraphApiEndpoint

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    // reserved: false = Windows (Dynamic Linux not available in all Azure Gov regions)
    reserved: false
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}' }
        // Windows Consumption plan requires WEBSITE_CONTENT* settings (Linux does not)
        { name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'WEBSITE_CONTENTSHARE', value: name }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'CosmosDbConnectionString', value: cosmosDbConnectionString }
        { name: 'StorageConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'CognitiveServicesEndpoint', value: cognitiveServicesEndpoint }
        { name: 'CognitiveServicesKey', value: cognitiveServicesKey }
        { name: 'AzureOpenAI__Endpoint', value: openAIEndpoint }
        { name: 'AzureOpenAI__ApiKey', value: openAIKey }
        { name: 'AzureOpenAI__DeploymentName', value: openAIDeploymentName }
        // ── Teams Calling Bot (Bot Framework) ──────────────────
        { name: 'MicrosoftAppType', value: 'SingleTenant' }
        { name: 'MicrosoftAppId', value: botAppId }
        { name: 'MicrosoftAppPassword', value: botAppPassword }
        { name: 'MicrosoftAppTenantId', value: botTenantId }
        // ── Gov-cloud Bot Framework endpoints (empty = commercial) ─
        { name: 'ChannelService', value: channelService }
        { name: 'OAuthUrl', value: oAuthUrl }
        // ── Microsoft Graph API endpoint ─────────────────────────
        { name: 'GraphApiEndpoint', value: graphApiEndpoint }        // ── MCP server (Phase 3 client integration reads these) ─────
        { name: 'Mcp__Enabled', value: string(mcpEnabled) }
        { name: 'Mcp__Endpoint', value: mcpEndpoint }
        { name: 'Mcp__Audience', value: mcpAudience }
        { name: 'Mcp__FallbackToDirect', value: 'true' }      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

@description('Function App default hostname (full HTTPS URL)')
output defaultHostname string = 'https://${functionApp.properties.defaultHostName}'

@description('Function App resource ID')
output resourceId string = functionApp.id

@description('Raw hostname (no scheme) — used to construct the Bot messaging endpoint')
output rawHostname string = functionApp.properties.defaultHostName

@description('System-assigned managed identity principal ID')
output principalId string = functionApp.identity.principalId
