// ─────────────────────────────────────────────────────────────────
// Azure Communication Services for telephony/PSTN
// ─────────────────────────────────────────────────────────────────

@description('ACS resource name')
param name string

@description('Location (must be "global" for ACS)')
param location string

@description('Resource tags')
param tags object

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    // Use 'usgov' for Azure Government, 'unitedstates' for commercial Azure
    dataLocation: environment().name == 'AzureUSGovernment' ? 'usgov' : 'unitedstates'
  }
}

@description('Communication Services connection string')
@secure()
output connectionString string = communicationService.listKeys().primaryConnectionString

@description('Communication Services resource ID')
output resourceId string = communicationService.id

@description('Communication Services hostname')
output hostname string = communicationService.properties.hostName
