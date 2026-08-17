// ─────────────────────────────────────────────────────────────────
// Key Vault — holds secrets referenced by App Settings via
// @Microsoft.KeyVault(SecretUri=...) so raw secrets never land in
// App Service configuration or source control.
// ─────────────────────────────────────────────────────────────────

@description('Key Vault name (max 24 characters)')
@maxLength(24)
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@secure()
@description('Cosmos DB connection string to store as the CosmosDbConnectionString secret')
param cosmosDbConnectionString string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

resource cosmosDbSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'CosmosDbConnectionString'
  properties: {
    value: cosmosDbConnectionString
  }
}

@description('Key Vault name')
output name string = keyVault.name

@description('Key Vault URI')
output uri string = keyVault.properties.vaultUri

@description('Key Vault reference string for the CosmosDbConnectionString secret, ready to use as an App Setting value')
output cosmosDbConnectionStringRef string = '@Microsoft.KeyVault(SecretUri=${cosmosDbSecret.properties.secretUri})'
