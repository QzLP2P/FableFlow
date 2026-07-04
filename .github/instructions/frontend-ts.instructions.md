---
applyTo: "frontend/**/*.{ts,tsx}"
---

# Frontend TypeScript / React / MUI — FableFlow

## Mobile-first (lecture)
- Concevoir d'abord pour mobile : single-column, texte de lecture confortable, interlignage aéré.
- Boutons de choix ≥ 48px de hauteur, pleine largeur sur mobile, espacés.
- Typographie responsive (breakpoints MUI / `clamp`), largeur de lecture limitée (`maxWidth` ~ 60–70ch).
- Respecter les safe-areas et le viewport mobile.

## React / TypeScript
- Composants fonctionnels + hooks. Pas de classe.
- `strict: true`, pas de `any`. Typer les props et les réponses API.
- État serveur via React Query. Pas de logique métier critique côté client.

## MUI
- Styliser via `sx` / `theme`, pas de CSS inline arbitraire ni de valeurs magiques.
- Centraliser le thème (palette, typographie, formes) dans `src/theme`.

## API & secrets
- Appels centralisés dans `src/api`. Types alignés sur les DTOs backend.
- Seule `VITE_API_BASE_URL` est exposée. Aucun secret côté client.
