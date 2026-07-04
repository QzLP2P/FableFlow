## Description

<!-- Décrire le changement et le contexte. Lier l'issue si applicable. -->

## Type de changement

- [ ] Feature
- [ ] Fix
- [ ] Refactor
- [ ] Infra / CI
- [ ] Docs

## Checklist qualité

- [ ] Build backend + frontend verts.
- [ ] Tests verts (`dotnet test`).
- [ ] Respect des règles de dépendances entre couches.
- [ ] Respect du pattern CQRS (handlers dépendent des ports).
- [ ] Aucun secret commité ; entrées validées aux frontières.
- [ ] Pas de logique métier dans les endpoints ni le frontend.
- [ ] Frontend mobile-first vérifié (le cas échéant).
- [ ] Tests ajoutés pour tout nouveau cas d'usage.
- [ ] Documentation mise à jour si le contrat change.
