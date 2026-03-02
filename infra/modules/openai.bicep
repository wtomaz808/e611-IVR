// ─────────────────────────────────────────────────────────────────
// Azure OpenAI — Intent classification for transcript-based routing
// ─────────────────────────────────────────────────────────────────

@description('Azure OpenAI account name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('OpenAI model deployment name')
param deploymentName string = 'gpt-41'

@description('OpenAI model name')
param modelName string = 'gpt-4.1'

@description('OpenAI model version')
param modelVersion string = '2025-04-14'

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    customSubDomainName: name
  }
}

resource gptDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAIAccount
  name: deploymentName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

@description('Azure OpenAI endpoint URL')
output endpoint string = openAIAccount.properties.endpoint

@description('Azure OpenAI primary key')
@secure()
output primaryKey string = openAIAccount.listKeys().key1

@description('Deployment name for use in application config')
output deploymentName string = gptDeployment.name
