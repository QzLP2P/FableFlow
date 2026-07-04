using 'main.bicep'

param appName = 'fableflow'
param environmentName = 'dev'
param staticWebAppLocation = 'westeurope'

// Vérifier la version disponible du modèle avant déploiement :
// az cognitiveservices account list-models --name <compte> --resource-group <rg>
param chatModelVersion = '2026-03-17'
