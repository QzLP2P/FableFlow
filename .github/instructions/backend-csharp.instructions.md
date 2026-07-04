---
applyTo: "backend/**/*.cs"
---

# Backend C# — FableFlow

## Architecture en couches
- Respecter les dépendances : `Api → Application → Domain`, `Infrastructure → Application/Domain`.
- `Domain` sans dépendance externe. `Application` sans dépendance à `Infrastructure`/`Api`.
- Les implémentations concrètes (Azure OpenAI, repositories) vivent uniquement dans `Infrastructure`.

## CQRS
- `record` pour commands/queries et DTOs. `sealed class` pour handlers.
- Un handler = une responsabilité. Il dépend des ports, jamais des implémentations.
- Validation via FluentValidation + `ValidationBehavior`. Jamais de validation dans le handler.

## Style
- `namespace` file-scoped, nullable activé, `using` System en premier.
- Champs privés `_camelCase` et `readonly` par défaut.
- Async pour l'I/O, suffixe `Async`, `CancellationToken` propagé.
- Injection par constructeur uniquement. Pas de `service locator`.

## Interdits
- Pas d'accès direct au SDK Azure OpenAI hors `Infrastructure`.
- Pas de logique métier dans les endpoints.
- Pas de secret en dur ; utiliser la configuration / Key Vault.

## Tests
- xUnit + FluentAssertions + NSubstitute. Couvrir toute nouvelle logique.
