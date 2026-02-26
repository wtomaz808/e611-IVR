// ─────────────────────────────────────────────────────────────────
// Storage Account for IVR audio prompts and Function App storage
// ─────────────────────────────────────────────────────────────────

@description('Storage account name (must be globally unique, lowercase, no hyphens)')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: take(replace(toLower(name), '-', ''), 24)
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-04-01' = {
  parent: storageAccount
  name: 'default'
}

resource promptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  parent: blobService
  name: 'ivr-prompts'
  properties: {
    publicAccess: 'None'
  }
}

@description('Storage account name')
output name string = storageAccount.name

@description('Storage account primary key')
@secure()
output primaryKey string = storageAccount.listKeys().keys[0].value

@description('Storage account connection string')
@secure()
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
