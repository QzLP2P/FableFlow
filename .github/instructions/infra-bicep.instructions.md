---
applyTo: "infra/**/*.bicep"
---

# Infra Bicep — FableFlow

## Principes

- Idempotent et paramétré. Pas de valeurs codées en dur spécifiques à un environnement.
- Utiliser des modules par ressource logique (`keyvault`, `appservice`, `staticwebapp`, `openai`, `monitoring`, `identity`).
- Exposer des `output` utiles (endpoints, noms de ressources, identités).

## Sécurité

- Managed Identity pour l'accès inter-services. Pas de clés d'accès stockées.
- Role assignments explicites (Key Vault Secrets User, Cognitive Services OpenAI User).
- Aucun secret en clair dans les fichiers ni dans les paramètres.

## Bonnes pratiques

- Nommage via `uniqueString()` pour l'unicité globale.
- `what-if` avant tout déploiement.
- Préférer les dernières `apiVersion` stables.
