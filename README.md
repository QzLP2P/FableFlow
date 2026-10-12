# FableFlow

Application web d'**histoires personnalisées à lire ensemble**, générées par LLM. On décrit le héros (prénom, âge, compagnons, goûts), on choisit un univers et une longueur, puis une idée de départ parmi plusieurs propositions (ou la sienne). FableFlow écrit l'histoire **chapitre par chapitre**, avec quelques **moments de choix** en chemin, jusqu'à une **conclusion narrative** : pas de victoire ni de défaite.

- **Adaptée à l'âge (3 à 12 ans)** : longueur des chapitres, vocabulaire, phrases et traitement des sujets suivent la tranche d'âge du héros.
- **Deux modes de lecture** : « un adulte, à voix haute » (texte plus riche) ou « l'enfant, tout seul » (texte plus simple, plus gros caractères).
- **Aides à la lecture** (mode enfant) : alternance de couleurs et arc sous chaque syllabe écrite, activables à tout moment.
- **Bibliothèque et famille locales** : les histoires terminées et les profils restent sur l'appareil, sans compte ni synchronisation.

## Stack

| Couche      | Technologie                                                                                  |
| ----------- | -------------------------------------------------------------------------------------------- |
| Frontend    | React + TypeScript + MUI (Vite), **mobile-first**, React Query                               |
| Backend     | .NET 10 Web API, SOLID + CQRS (MediatR + FluentValidation)                                   |
| LLM         | Azure OpenAI / Foundry (texte), FLUX.2-pro (illustrations, facultatif), abstraits via ports  |
| Persistance | Serveur : in-memory (MVP) via `IAdventureRepository` · Client : localStorage + IndexedDB     |
| Secrets     | Azure Key Vault + Managed Identity                                                           |
| CI/CD       | GitHub Actions (OIDC)                                                                        |
| Hébergement | Azure Static Web App (front) + App Service (API)                                             |

## Structure du dépôt

```
fableflow/
  .github/          # Copilot instructions + workflows CI/CD
  backend/          # Solution .NET 10 en couches Domain/Application/Infrastructure/Api
  frontend/         # App React + MUI (Vite)
  infra/bicep/      # Infrastructure Azure (Bicep)
  docs/             # Documentation d'architecture
```

## Démarrage rapide

### Backend

```powershell
cd backend
dotnet restore
dotnet build
dotnet run --project src/FableFlow.Api
```

L'API écoute par défaut sur `https://localhost:7080` (Swagger sur `/swagger`).

> ⚠️ Le stockage serveur est **en mémoire** : une histoire en cours est perdue au redémarrage de l'API. Les histoires **terminées** sont conservées côté navigateur (bibliothèque locale).

Tests : `dotnet test backend/FableFlow.slnx`.

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

Configurez `VITE_API_BASE_URL` dans `frontend/.env` (voir `.env.example`).

Tests, build et lint : `npm test`, `npm run build`, `npm run lint`.

## Configuration

Les secrets ne sont **jamais** commités. En local, utilisez les _user secrets_ .NET ou des variables d'environnement. En Azure, ils sont résolus depuis Key Vault via `DefaultAzureCredential`.

| Clé                                    | Description                                                                                                                         |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `AzureOpenAI:Endpoint`                 | Endpoint du compte Azure OpenAI / Foundry                                                                                           |
| `AzureOpenAI:ChatDeployment`           | Déploiement qui écrit les chapitres (défaut `gpt-5.4-mini`)                                                                         |
| `AzureOpenAI:ReasoningEffort`          | Effort de raisonnement des chapitres (défaut `none`). `minimal` est refusé à partir de `gpt-5.1`. **Vide** pour un modèle sans raisonnement (`gpt-4.1`, `gpt-4o`…) |
| `AzureOpenAI:PremiseDeployment`        | Déploiement des idées de départ (tâche courte, défaut `gpt-5.4-nano`). Vide : `ChatDeployment`                                      |
| `AzureOpenAI:PremiseReasoningEffort`   | Effort des idées de départ. Absent : reprend `ReasoningEffort` ; vide : non envoyé                                                  |
| `AzureOpenAI:ImageDeployment`          | Déploiement `gpt-image-*` (alternative à FLUX, non branchée par défaut)                                                             |
| `Flux:Endpoint`, `Flux:DeploymentName` | Déploiement FLUX.2-pro des illustrations                                                                                            |
| `Features:ImageGeneration`             | Active les illustrations (désactivé par défaut)                                                                                     |
| `Cors:AllowedOrigins`                  | Origines autorisées à appeler l'API (URL du frontend)                                                                               |
| `KeyVault:Uri`                         | Key Vault d'où charger les secrets en Azure                                                                                         |

### Changer de modèle

Le choix du modèle est **purement configuration** : déployez le modèle dans le compte Foundry visé par `AzureOpenAI:Endpoint`, puis renseignez `ChatDeployment` (ou `PremiseDeployment`) et `ReasoningEffort` selon ce que le modèle accepte. Le client appelle l'API Chat Completions d'Azure OpenAI en mode JSON.

| Modèle               | Usage                                      | `ReasoningEffort` conseillé |
| -------------------- | ------------------------------------------ | --------------------------- |
| `gpt-5.4-mini`       | Chapitres (défaut)                         | `none` (ou `low`)           |
| `gpt-5.4-nano`       | Idées de départ (défaut)                   | `none`                      |
| `gpt-5.6-terra`      | Challenger qualité des chapitres           | `none` ou `low`             |
| `gpt-5.6-luna`       | Alternative rapide et économique           | `none` ou `low`             |
| `gpt-6.1-sol`        | Haut de gamme, surdimensionné ici          | `none` ou `low`             |
| `gpt-4.1` / `gpt-4o` | Modèles sans raisonnement                  | **vide**                    |

Renseignez toujours `ReasoningEffort` avec `gpt-5.6` : sans valeur, il raisonne en `medium` (plus lent et plus cher). `minimal` n'est accepté que par les premiers `gpt-5` (`gpt-5`, `gpt-5-mini`, `gpt-5-nano`).

En production, les déploiements et les variables `AzureOpenAI__ChatDeployment` / `AzureOpenAI__PremiseDeployment` sont posés par Bicep (`infra/bicep/modules/openai.bicep`) : une modification manuelle dans l'App Service est écrasée au prochain `deploy-infra`.

Les modèles tiers (Grok, DeepSeek, Mistral…) n'ont pas été validés. `grok-4.7` en particulier est documenté en anglais seulement, refuse `none` (`low` au minimum, `high` par défaut) et est documenté sur l'endpoint `/openai/v1` plutôt que sur celui qu'utilise le client. Évitez `model-router` : le ton et la longueur des chapitres varieraient d'un appel à l'autre.

## Documentation

- [Architecture](docs/architecture.md)
- [Consignes Copilot](.github/copilot-instructions.md)

## Note propriété intellectuelle

Les thèmes `pokemon` et `spidey` sont des **placeholders** de cadrage. Avant toute diffusion publique, remplacez-les par des univers originaux ou sous licence.
