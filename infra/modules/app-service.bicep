// ─────────────────────────────────────────────────────────────────
// Azure App Service — Blazor Admin Portal
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

@secure()
@description('Storage account connection string')
param storageConnectionString string

@description('App Insights connection string')
param appInsightsConnectionString string

@secure()
@description('Azure AD tenant ID')
param azureAdTenantId string

@secure()
@description('Azure AD client ID')
param azureAdClientId string

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

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        { name: 'CosmosDbConnectionString', value: cosmosDbConnectionString }
        { name: 'StorageConnectionString', value: storageConnectionString }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'AzureAd__TenantId', value: azureAdTenantId }
        { name: 'AzureAd__ClientId', value: azureAdClientId }
        { name: 'AzureAd__Instance', value: environment().authentication.loginEndpoint }
        { name: 'AzureAd__CallbackPath', value: '/signin-oidc' }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

@description('Admin Portal default hostname')
output defaultHostname string = 'https://${appService.properties.defaultHostName}'

@description('App Service resource ID')
output resourceId string = appService.id

@description('App Service Plan resource ID')
output planId string = appServicePlan.id
