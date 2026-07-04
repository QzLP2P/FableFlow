# FableFlow

Application web d'**histoires interactives générées par LLM**. L'utilisateur choisit un thème et un nombre de scènes, puis avance dans le récit via des choix successifs qui influencent la suite jusqu'à une **victoire**, une **défaite**, ou une **fin narrative**.

## Stack

| Couche | Technologie |
| --- | --- |
| Frontend | React + TypeScript + MUI (Vite), **mobile-first** |
| Backend | .NET 10 Web API, SOLID + CQRS (MediatR) |
| LLM | Azure OpenAI (texte + image `gpt-image-1`), abstrait via ports |
| Persistance | In-memory (MVP), abstraite via `IAdventureRepository` |
| Secrets | Azure Key Vault + Managed Identity |
| CI/CD | GitHub Actions (OIDC) |
| Hébergement | Azure Static Web App (front) + App Service (API) |

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

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

Configurez `VITE_API_BASE_URL` dans `frontend/.env` (voir `.env.example`).

## Configuration

Les secrets ne sont **jamais** commités. En local, utilisez les *user secrets* .NET ou des variables d'environnement. En Azure, ils sont résolus depuis Key Vault via `DefaultAzureCredential`.

| Clé | Description |
| --- | --- |
| `AzureOpenAI:Endpoint` | Endpoint du compte Azure OpenAI |
| `AzureOpenAI:ChatDeployment` | Déploiement du modèle orchestrateur (ex. `gpt-4o`) |
| `AzureOpenAI:ImageDeployment` | Déploiement `gpt-image-1` |
| `Features:ImageGeneration` | Active/désactive la génération d'images |

## Documentation

- [Architecture](docs/architecture.md)
- [Consignes Copilot](.github/copilot-instructions.md)

## Note propriété intellectuelle

Les thèmes `pokemon` et `spidey` sont des **placeholders** de cadrage. Avant toute diffusion publique, remplacez-les par des univers originaux ou sous licence.
