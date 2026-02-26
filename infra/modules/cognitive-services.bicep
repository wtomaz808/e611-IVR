// ─────────────────────────────────────────────────────────────────
// Azure Cognitive Services — Speech (TTS + STT for IVR)
// ─────────────────────────────────────────────────────────────────

@description('Cognitive Services account name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

resource speechAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'SpeechServices'
  sku: {
    name: 'S0'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    customSubDomainName: name
  }
}

@description('Speech Services endpoint URL')
output endpoint string = speechAccount.properties.endpoint

@description('Speech Services primary key')
@secure()
output primaryKey string = speechAccount.listKeys().key1
