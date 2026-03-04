// ─────────────────────────────────────────────────────────────────
// Azure App Service — PSTN Simulator
// ─────────────────────────────────────────────────────────────────

@description('App Service name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('App Service Plan resource ID (shared with admin portal)')
param appServicePlanId string

@description('IVR Function App endpoint for callbacks')
param ivrEndpoint string

@description('ACS Mode: Mock or Live')
param acsMode string = 'Mock'

@secure()
@description('ACS connection string (optional, for Live mode)')
param acsConnectionString string = ''

@description('ACS caller number (optional, for Live mode)')
param acsCallerNumber string = ''

resource simulatorApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        { name: 'AcsMode', value: acsMode }
        { name: 'AcsConnectionString', value: acsConnectionString }
        { name: 'AcsCallerNumber', value: acsCallerNumber }
        { name: 'IvrEndpoint', value: ivrEndpoint }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}

@description('Simulator app default hostname')
output defaultHostname string = 'https://${simulatorApp.properties.defaultHostName}'

@description('Simulator App Service resource ID')
output resourceId string = simulatorApp.id
