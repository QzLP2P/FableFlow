# Consignes Copilot — FableFlow

FableFlow est une application d'histoires interactives générées par LLM : **frontend React + TypeScript + MUI (mobile-first)** et **backend .NET 10 Web API** structuré en couches avec **SOLID + CQRS**, déployé sur **Azure** via **GitHub Actions**, secrets dans **Azure Key Vault**.

## Architecture

Le backend suit une architecture en couches à dépendances **unidirectionnelles** :

```
Api  →  Application  →  Domain
Infrastructure  →  Application (implémente ses ports) + Domain
```

- **Domain** : entités, value objects, enums, logique métier pure. **Aucune** dépendance externe (pas de framework, pas d'I/O).
- **Application** : cas d'usage CQRS (commands/queries + handlers), DTOs, **interfaces (ports)** vers l'extérieur, validation. Ne dépend que de `Domain`.
- **Infrastructure** : implémentations concrètes des ports (Azure OpenAI, repositories, prompt builder). Dépend de `Application` et `Domain`.
- **Api** : composition root, endpoints HTTP, DI, config, observabilité. Dépend de `Application` et `Infrastructure`.

### Règles de dépendances (interdits)
- ❌ `Domain` ne référence **jamais** `Application`, `Infrastructure` ou `Api`.
- ❌ `Application` ne référence **jamais** `Infrastructure` ni `Api`.
- ❌ Pas d'appel direct au SDK Azure OpenAI en dehors de `Infrastructure`.
- ❌ Pas de logique métier dans les endpoints ou dans le frontend.

## Règles CQRS

- Une **commande** modifie l'état (`StartAdventureCommand`, `MakeChoiceCommand`) et renvoie un DTO.
- Une **requête** est en lecture seule (`GetAdventureSessionQuery`, `GetAvailableThemesQuery`, `GetAdventureHistoryQuery`).
- Un handler par command/query, orchestré via **MediatR**.
- Validation via **FluentValidation** dans un `ValidationBehavior` (pipeline), jamais dans le handler.
- Les handlers dépendent des **ports** (`IStoryGenerationService`, `IImageGenerationService`, `IPromptBuilder`, `IAdventureRepository`, `IThemePolicyProvider`), jamais des implémentations.

## Conventions C#

- `namespace` en **file-scoped**, `using` triés, System en premier.
- Nullable activé, warnings as errors sur le nullable.
- Champs privés préfixés `_camelCase`, immuables (`readonly`) par défaut.
- Préférer `record` pour DTOs/commands/queries, `sealed class` pour handlers et services.
- Async partout pour l'I/O : suffixe `Async`, propager `CancellationToken`.
- Injection par constructeur uniquement.

## Conventions TypeScript / React / MUI

- **Mobile-first** : concevoir d'abord pour le mobile (lecture confortable, gros boutons ≥ 48px, single-column), puis élargir via breakpoints MUI.
- Composants fonctionnels + hooks. Pas de classe.
- Typage strict (`strict: true`), pas de `any`.
- Style via le système MUI (`sx`, `theme`), pas de CSS inline arbitraire ni de valeurs magiques.
- Appels API centralisés dans `src/api`, données serveur gérées via React Query.
- Aucun secret côté client ; seule `VITE_API_BASE_URL` est exposée.

## Prompts IA

- Toute construction de prompt passe par `IPromptBuilder` et des `PromptTemplate` **versionnés**.
- Un prompt inclut toujours : thème, public cible, niveau de vocabulaire, résumé des scènes précédentes, état de session, choix utilisateur, contraintes de sécurité, structure de scène attendue, conditions de victoire/défaite.
- **Garde-fous enfant** : pour un `AudienceTarget.Child`, vocabulaire simple, ton sûr, 2–3 choix non ambigus, aucun contenu inapproprié (texte **et** image).

## Sécurité & secrets

- Aucun secret dans le code, les logs ou le repo. Résolution via Key Vault + `DefaultAzureCredential`.
- Valider toutes les entrées aux frontières (endpoints).
- Respecter l'OWASP Top 10. Erreurs renvoyées en `ProblemDetails`, sans fuite de détails internes.

## Tests

- **xUnit + FluentAssertions + NSubstitute**.
- Domain : logique de transition (victoire/défaite/mauvais choix).
- Application : handlers avec ports mockés.
- Api : tests d'intégration via `WebApplicationFactory`.
- Tout nouveau cas d'usage doit être couvert par des tests.

## GitHub Actions & Azure

- Authentification **OIDC** (federated credentials), pas de secrets longue durée.
- Un workflow par cible : `ci`, `deploy-infra`, `deploy-backend`, `deploy-frontend`.
- Infra en **Bicep** idempotent ; `what-if` avant déploiement.

## Standards de Pull Request

- Build + tests verts. Respect strict des règles de dépendances et de CQRS.
- Pas de code mort, pas de secret, pas de logique dans les endpoints.
- Nouvelle fonctionnalité = tests associés + mise à jour de la doc si le contrat change.
