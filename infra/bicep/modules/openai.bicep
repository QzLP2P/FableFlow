@description('Nom de base utilisé pour dériver le nom du compte Foundry.')
param appName string

@description('Emplacement Azure du compte Azure AI Foundry (vérifier la disponibilité des modèles choisis).')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

@description('Object id de l\'identité managée à autoriser à appeler les modèles.')
param identityPrincipalId string

@description('Nom du déploiement du modèle de génération narrative (ex. gpt-5.4-mini).')
param chatModelName string = 'gpt-5.4-mini'

@description('Version du modèle de chat à déployer.')
param chatModelVersion string

@description('Capacité (unités de débit) du déploiement de chat.')
param chatCapacity int = 10

@description('Nom du déploiement du modèle de génération d\'image (ex. FLUX.2-pro).')
param imageModelName string = 'FLUX.2-pro'

@description('Format/éditeur du modèle d\'image tel qu\'enregistré dans le catalogue Foundry (vérifié via `az cognitiveservices model list --location <region>`).')
param imageModelFormat string = 'Black Forest Labs'

@description('Version du modèle d\'image à déployer.')
param imageModelVersion string = '1'

@description('Capacité (unités de débit) du déploiement d\'image. Une capacité de 1 provoque des HTTP 429 (limitation de débit) fréquents dès qu\'une aventure enchaîne plusieurs scènes (constaté en production via les dépendances Application Insights) ; le code retente désormais automatiquement (voir FluxImageGenerationService), mais une capacité plus généreuse réduit la fréquence des échecs définitifs.')
param imageCapacity int = 3

var cognitiveServicesOpenAiUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

var accountName = 'aif-${toLower(appName)}-${uniqueString(resourceGroup().id)}'

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: chatModelName
  sku: {
    name: 'GlobalStandard'
    capacity: chatCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

resource imageDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: imageModelName
  sku: {
    name: 'GlobalStandard'
    capacity: imageCapacity
  }
  properties: {
    model: {
      format: imageModelFormat
      name: imageModelName
      version: imageModelVersion
    }
  }
  dependsOn: [
    chatDeployment
  ]
}

resource openAiUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, identityPrincipalId, cognitiveServicesOpenAiUserRoleId)
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      cognitiveServicesOpenAiUserRoleId
    )
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource cognitiveServicesUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, identityPrincipalId, cognitiveServicesUserRoleId)
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('Endpoint Azure OpenAI (data plane openai.azure.com) du compte.')
output openAiEndpoint string = 'https://${account.name}.openai.azure.com/'

@description('Endpoint Azure AI Foundry (data plane services.ai.azure.com) du compte, utilisé pour les modèles partenaires (ex. FLUX).')
output foundryEndpoint string = 'https://${account.name}.services.ai.azure.com/'

@description('Id de ressource du compte Foundry.')
output accountId string = account.id
