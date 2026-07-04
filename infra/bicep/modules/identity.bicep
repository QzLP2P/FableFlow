@description('Nom de base utilisé pour dériver le nom de l\'identité managée.')
param appName string

@description('Emplacement Azure de l\'identité managée.')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${appName}'
  location: location
  tags: tags
}

@description('Id de ressource de l\'identité managée.')
output identityId string = identity.id

@description('Principal id (object id) de l\'identité managée, pour les attributions de rôle.')
output principalId string = identity.properties.principalId

@description('Client id de l\'identité managée.')
output clientId string = identity.properties.clientId
