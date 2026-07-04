targetScope = 'resourceGroup'

@description('Nom de base de l\'application, utilisé pour dériver les noms de ressources.')
param appName string = 'fableflow'

@description('Nom de l\'environnement (dev, staging, prod), utilisé pour les étiquettes.')
param environmentName string = 'dev'

@description('Emplacement Azure principal des ressources.')
param location string = resourceGroup().location

@description('Emplacement Azure de la Static Web App (liste de régions restreinte, ex. westeurope, eastus2, centralus).')
param staticWebAppLocation string = 'westeurope'

@description('Version du modèle de chat Azure OpenAI à déployer (ex. 2026-03-17 pour gpt-5.4-mini).')
param chatModelVersion string

@description('Origine autorisée pour le CORS de l\'API (URL de la Static Web App). Laisser vide pour la déduire automatiquement après déploiement.')
param corsAllowedOrigin string = ''

var tags = {
  application: appName
  environment: environmentName
}

module monitoring 'modules/monitoring.bicep' = {
  params: {
    appName: appName
    location: location
    tags: tags
  }
}

module identity 'modules/identity.bicep' = {
  params: {
    appName: appName
    location: location
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  params: {
    location: location
    tags: tags
    identityPrincipalId: identity.outputs.principalId
  }
}

module foundry 'modules/openai.bicep' = {
  params: {
    appName: appName
    location: location
    tags: tags
    identityPrincipalId: identity.outputs.principalId
    chatModelVersion: chatModelVersion
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  params: {
    appName: appName
    location: staticWebAppLocation
    tags: tags
  }
}

module appService 'modules/appservice.bicep' = {
  params: {
    appName: appName
    location: location
    tags: tags
    userAssignedIdentityId: identity.outputs.identityId
    userAssignedIdentityClientId: identity.outputs.clientId
    keyVaultUri: keyVault.outputs.vaultUri
    azureOpenAiEndpoint: foundry.outputs.openAiEndpoint
    chatDeploymentName: 'gpt-5.4-mini'
    fluxEndpoint: foundry.outputs.foundryEndpoint
    fluxDeploymentName: 'FLUX.2-pro'
    fluxModelSlug: 'flux-2-pro'
    imageGenerationEnabled: true
    allowedOrigin: empty(corsAllowedOrigin) ? 'https://${staticWebApp.outputs.defaultHostName}' : corsAllowedOrigin
    appInsightsConnectionString: monitoring.outputs.connectionString
  }
}

@description('URL de l\'API déployée.')
output apiUrl string = 'https://${appService.outputs.defaultHostName}'

@description('URL du frontend déployé.')
output frontendUrl string = 'https://${staticWebApp.outputs.defaultHostName}'

@description('Nom de la Static Web App (pour récupérer le jeton de déploiement dans le pipeline CI/CD).')
output staticWebAppName string = staticWebApp.outputs.name

@description('URI du coffre Key Vault.')
output keyVaultUri string = keyVault.outputs.vaultUri
