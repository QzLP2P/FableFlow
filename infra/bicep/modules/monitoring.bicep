@description('Nom de base utilisé pour dériver les noms de ressources.')
param appName string

@description('Emplacement Azure des ressources de supervision.')
param location string

@description('Étiquettes communes appliquées aux ressources.')
param tags object

var logAnalyticsName = 'log-${appName}'
var appInsightsName = 'appi-${appName}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

@description('Chaîne de connexion Application Insights (à transmettre en app setting).')
output connectionString string = appInsights.properties.ConnectionString

@description('Id du workspace Log Analytics.')
output logAnalyticsWorkspaceId string = logAnalytics.id
