// ─────────────────────────────────────────────────────────────────
// Grants "Key Vault Secrets User" (read-only secret access) to a set
// of managed identity principal IDs on an existing Key Vault. Deployed
// after the Function App / App Service so their system-assigned
// identities already exist.
// ─────────────────────────────────────────────────────────────────

@description('Name of the existing Key Vault to grant access to')
param keyVaultName string

@description('Principal IDs (managed identities) to grant Key Vault Secrets User')
param principalIds array

var secretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secretReaderRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in principalIds: {
    name: guid(keyVault.id, principalId, secretsUserRoleId)
    scope: keyVault
    properties: {
      roleDefinitionId: secretsUserRoleId
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]
