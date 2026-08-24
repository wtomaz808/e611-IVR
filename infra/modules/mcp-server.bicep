// ─────────────────────────────────────────────────────────────────
// Azure App Service — MCP Server (remote MCP tool host)
// Hosts the IVR.McpServer project over Streamable HTTP. Always On is
// required — this sits on the live-call path via the Function App.
// ─────────────────────────────────────────────────────────────────

@description('App Service name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@secure()
@description('Cosmos DB connection string')
param cosmosDbConnectionString string

@description('App Insights connection string')
param appInsightsConnectionString string

@description('Require Entra bearer-token authentication on the MCP endpoint. Only disable for isolated dev/test.')
param requireAuthentication bool = true

@description('Entra tenant ID that issues tokens for MCP callers')
param tenantId string = ''

@description('Entra audience (API application ID URI) that MCP validates tokens against')
param audience string = ''

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource mcpServerApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      appSettings: [
        { name: 'CosmosDbConnectionString', value: cosmosDbConnectionString }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'Mcp__RequireAuthentication', value: string(requireAuthentication) }
        { name: 'Mcp__TenantId', value: tenantId }
        { name: 'Mcp__Audience', value: audience }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

@description('MCP Server default hostname (full HTTPS URL)')
output defaultHostname string = 'https://${mcpServerApp.properties.defaultHostName}'

@description('MCP Server resource ID')
output resourceId string = mcpServerApp.id

@description('System-assigned managed identity principal ID')
output principalId string = mcpServerApp.identity.principalId
