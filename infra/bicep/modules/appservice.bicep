@description('Nom de base utilisé pour dériver les noms de ressources App Service.')
param appName string

@description('Emplacement Azure du plan et du site.')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

@description('Id de ressource de l\'identité managée utilisateur à attacher au site.')
param userAssignedIdentityId string

@description('Client id de l\'identité managée (utilisé par DefaultAzureCredential).')
param userAssignedIdentityClientId string

@description('URI du coffre Key Vault contenant les secrets applicatifs.')
param keyVaultUri string

@description('Endpoint Azure OpenAI du compte Foundry.')
param azureOpenAiEndpoint string

@description('Nom du déploiement du modèle de chat.')
param chatDeploymentName string

@description('Endpoint Azure AI Foundry pour les modèles partenaires (FLUX).')
param fluxEndpoint string

@description('Nom du déploiement du modèle d\'image FLUX.')
param fluxDeploymentName string

@description('Slug du modèle FLUX dans l\'URL du fournisseur BFL.')
param fluxModelSlug string

@description('Active la génération d\'images.')
param imageGenerationEnabled bool

@description('Origine autorisée pour le CORS (URL de la Static Web App).')
param allowedOrigin string

@description('Chaîne de connexion Application Insights.')
@secure()
param appInsightsConnectionString string

var planName = 'plan-${appName}'
var siteName = 'app-${appName}-${uniqueString(resourceGroup().id)}'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: siteName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AZURE_CLIENT_ID', value: userAssignedIdentityClientId }
        { name: 'KeyVault__Uri', value: keyVaultUri }
        { name: 'AzureOpenAI__Endpoint', value: azureOpenAiEndpoint }
        { name: 'AzureOpenAI__UseManagedIdentity', value: 'true' }
        { name: 'AzureOpenAI__ChatDeployment', value: chatDeploymentName }
        { name: 'Flux__Endpoint', value: fluxEndpoint }
        { name: 'Flux__UseManagedIdentity', value: 'true' }
        { name: 'Flux__DeploymentName', value: fluxDeploymentName }
        { name: 'Flux__ModelSlug', value: fluxModelSlug }
        { name: 'Features__ImageGeneration', value: string(imageGenerationEnabled) }
        { name: 'Cors__AllowedOrigins__0', value: allowedOrigin }
        { name: 'ApplicationInsights__ConnectionString', value: appInsightsConnectionString }
        { name: 'WEBSITES_PORT', value: '8080' }
      ]
    }
  }
}

@description('Nom d\'hôte par défaut du site App Service.')
output defaultHostName string = site.properties.defaultHostName

@description('Id de ressource du site.')
output siteId string = site.id
