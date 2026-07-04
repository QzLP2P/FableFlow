@description('Nom de base utilisé pour dériver le nom de la Static Web App.')
param appName string

@description('Emplacement Azure de la Static Web App (liste de régions restreinte).')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'swa-${appName}'
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: 'frontend'
      outputLocation: 'dist'
    }
  }
}

@description('Nom d\'hôte par défaut de la Static Web App.')
output defaultHostName string = staticWebApp.properties.defaultHostname

@description('Id de ressource de la Static Web App.')
output staticWebAppId string = staticWebApp.id

@description('Nom de la Static Web App (nécessaire pour récupérer le jeton de déploiement dans le pipeline).')
output name string = staticWebApp.name
