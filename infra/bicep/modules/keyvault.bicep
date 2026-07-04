@description('Emplacement Azure du coffre.')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

@description('Object id de l\'identité managée à autoriser en lecture des secrets.')
param identityPrincipalId string

@description('Id du tenant Microsoft Entra courant.')
param tenantId string = subscription().tenantId

var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

resource secretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identityPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('URI du coffre Key Vault.')
output vaultUri string = keyVault.properties.vaultUri

@description('Id de ressource du coffre.')
output vaultId string = keyVault.id
