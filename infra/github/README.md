# Déploiement Azure via GitHub Actions (OIDC)

Ce dossier documente la configuration Azure/GitHub nécessaire aux workflows `.github/workflows/deploy-*.yml`. Aucun secret longue durée n'est utilisé : l'authentification passe par **OIDC (federated credentials)**.

> **Statut** : déjà configuré pour ce repository (`QzLP2P/FableFlow`) le 2026-07-04. Cette section documente ce qui a été fait et sert de référence si la configuration doit être reproduite (autre repo, rotation, etc.).

## 1. Créer le principal de déploiement (une seule fois)

Ce principal est **différent** de l'identité managée applicative créée par `infra/bicep/modules/identity.bicep` (celle-ci sert uniquement à l'application pour appeler Key Vault/Azure AI Foundry à l'exécution). Le principal ci-dessous sert uniquement à GitHub Actions pour déployer.

> ⚠️ **Vérifier la branche par défaut du repo avant de créer les federated credentials** (`gh repo view <repo> --json defaultBranchRef --jq '.defaultBranchRef.name'`). Les exemples ci-dessous utilisent `main`, mais **QzLP2P/FableFlow utilise `master`** (voir section « Configuration actuelle ») — le subject du federated credential et le déclencheur `push.branches` des workflows doivent correspondre exactement, sinon `azure/login@v2` échoue silencieusement (aucun credential ne correspond) et les workflows ne se déclenchent jamais.

### Option A — App registration (nécessite un droit annuaire Microsoft Entra)

```powershell
$appName = "fableflow-github-deploy"
$repo = "<org-ou-user>/<repo>"       # ex. "QzLP2P/FableFlow"
$subscriptionId = "<subscription-id>"
$resourceGroup = "rg-fableflow"

$app = az ad app create --display-name $appName | ConvertFrom-Json
az ad sp create --id $app.appId

az ad app federated-credential create --id $app.appId --parameters '{
  "name": "github-main-branch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:'"$repo"':ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Si l'erreur `Insufficient privileges to complete the operation` apparaît (tenant d'entreprise où l'auto-inscription d'applications est désactivée — c'est le cas du tenant Bouygues Telecom Business Solution utilisé ici), utiliser l'option B.

### Option B — Identité managée affectée par l'utilisateur (utilisée pour ce repo)

Une identité managée supporte aussi les federated credentials OIDC, et ne nécessite qu'un droit `Contributor` sur le resource group (pas de droit annuaire) :

```powershell
$repo = "QzLP2P/FableFlow"
$resourceGroup = "rg-fableflow"
$location = "westeurope"

az group create --name $resourceGroup --location $location

az identity create --name id-fableflow-github-deploy --resource-group $resourceGroup --location $location

az identity federated-credential create --name github-master-branch `
  --identity-name id-fableflow-github-deploy --resource-group $resourceGroup `
  --issuer "https://token.actions.githubusercontent.com" `
  --subject "repo:$repo`:ref:refs/heads/master" `
  --audiences "api://AzureADTokenExchange"

az identity federated-credential create --name github-production-environment `
  --identity-name id-fableflow-github-deploy --resource-group $resourceGroup `
  --issuer "https://token.actions.githubusercontent.com" `
  --subject "repo:$repo`:environment:production" `
  --audiences "api://AzureADTokenExchange"
```

### Attribution des rôles (les deux options)

```powershell
# principalId = object id du service principal (option A) ou de l'identité managée (option B)
az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal `
  --role "Contributor" --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup"
az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal `
  --role "User Access Administrator" --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup"
```

`User Access Administrator` est nécessaire car `main.bicep` crée aussi des attributions de rôle RBAC (Key Vault Secrets User, Cognitive Services OpenAI User, etc.) pour l'identité managée applicative.

## Configuration actuelle (QzLP2P/FableFlow)

| Ressource               | Valeur                                                                                                |
| ----------------------- | ----------------------------------------------------------------------------------------------------- |
| Tenant Microsoft Entra  | Bouygues Telecom Business Solution (`37f626ab-77a8-4087-9c44-ef395de90a98`)                           |
| Abonnement Azure        | Visual Studio Enterprise (`a605300a-0c9e-4b29-b63c-607436903b70`)                                     |
| Resource group          | `rg-fableflow` (`westeurope`)                                                                         |
| Identité de déploiement | Identité managée `id-fableflow-github-deploy` (Option B, app registration indisponible sur ce tenant) |
| Federated credentials   | `repo:QzLP2P/FableFlow:ref:refs/heads/master` + `repo:QzLP2P/FableFlow:environment:production`        |
| Rôles attribués         | `Contributor` + `User Access Administrator` sur `rg-fableflow`                                        |
| Environnement GitHub    | `production` créé sur le repo                                                                         |

> ⚠️ La branche par défaut de ce repo est **`master`**, pas `main`. Les federated credentials et les déclencheurs `push` des workflows ciblent `master`. Si vous renommez la branche par défaut, mettez à jour les deux en même temps.

## 2. Secrets et variables GitHub à configurer

Dans **Settings → Secrets and variables → Actions** du repository (déjà fait pour QzLP2P/FableFlow via `gh secret set`/`gh variable set`) :

### Secrets (`Secrets` tab)

| Nom                               | Valeur                                                        | Statut       |
| --------------------------------- | ------------------------------------------------------------- | ------------ |
| `AZURE_CLIENT_ID`                 | `clientId` de l'identité managée `id-fableflow-github-deploy` | ✅ Configuré |
| `AZURE_TENANT_ID`                 | `37f626ab-77a8-4087-9c44-ef395de90a98`                        | ✅ Configuré |
| `AZURE_SUBSCRIPTION_ID`           | `a605300a-0c9e-4b29-b63c-607436903b70`                        | ✅ Configuré |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Jeton de déploiement de la Static Web App                     | ✅ Configuré |

### Variables (`Variables` tab, non sensibles)

| Nom                    | Valeur                                                  | Statut       |
| ---------------------- | ------------------------------------------------------- | ------------ |
| `AZURE_RESOURCE_GROUP` | `rg-fableflow`                                          | ✅ Configuré |
| `AZURE_LOCATION`       | `westeurope`                                            | ✅ Configuré |
| `API_BASE_URL`         | `https://app-fableflow-q6hxb5lxsgsdo.azurewebsites.net` | ✅ Configuré |

## 3. Récupérer le jeton de déploiement de la Static Web App

Après un premier passage de `deploy-infra` :

```powershell
az staticwebapp secrets list --name <nom-swa-sortie-staticWebAppName> `
  --resource-group rg-fableflow --query properties.apiKey -o tsv
```

Copier la valeur dans le secret GitHub `AZURE_STATIC_WEB_APPS_API_TOKEN` (`gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --repo QzLP2P/FableFlow`).

## 4. Ordre de déploiement recommandé

1. `deploy-infra` (crée Key Vault, Azure AI Foundry, App Service, Static Web App, identité managée applicative).
2. Récupérer les sorties (`apiUrl`, `staticWebAppName`) et renseigner `API_BASE_URL` + `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. `deploy-backend` et `deploy-frontend` (peuvent ensuite tourner indépendamment à chaque push).

## Déploiement effectué (2026-07-04)

| Composant | URL                                                   |
| --------- | ----------------------------------------------------- |
| API       | https://app-fableflow-q6hxb5lxsgsdo.azurewebsites.net |
| Frontend  | https://lemon-ground-09d7f7803.7.azurestaticapps.net  |
| Key Vault | https://kv-q6hxb5lxsgsdo.vault.azure.net/             |

Vérifié en production : `/api/themes`, `/health` OK côté API ; le frontend charge bien les thèmes depuis l'API (CORS opérationnel).

## Notes de sécurité

- Aucune clé API Azure OpenAI/Foundry n'est nécessaire en production : l'App Service utilise son identité managée (`UseManagedIdentity: true`) pour s'authentifier auprès d'Azure OpenAI et d'Azure AI Foundry.
- Le principal de déploiement OIDC n'a de droits que sur le groupe de ressources ciblé (scope minimal), jamais sur l'abonnement entier.
