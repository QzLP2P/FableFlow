import type { HeroInput } from "../api/types";

/** Bornes alignées sur le domaine backend (HeroProfile, AdventureSession). */
export const HERO_LIMITS = {
  minAge: 3,
  maxAge: 12,
  maxNameLength: 40,
  maxCompanions: 3,
  maxCompanionLength: 60,
  maxDetailLength: 200,
} as const;

export const MAX_PREMISE_LENGTH = 600;
export const MAX_PREMISE_TITLE_LENGTH = 120;

export const HERO_AGES: readonly number[] = Array.from(
  { length: HERO_LIMITS.maxAge - HERO_LIMITS.minAge + 1 },
  (_, index) => HERO_LIMITS.minAge + index,
);

/** Saisie en cours du héros (formulaire) ; l'âge reste vide tant qu'il n'est pas choisi. */
export interface HeroDraft {
  name: string;
  age: number | null;
  companions: string[];
  interests: string;
  personality: string;
  appearance: string;
}

export const emptyHeroDraft: HeroDraft = {
  name: "",
  age: null,
  companions: [],
  interests: "",
  personality: "",
  appearance: "",
};

export type HeroField = "name" | "age" | "companions" | "interests" | "personality" | "appearance";
export type HeroFieldErrors = Partial<Record<HeroField, string>>;

/** Même normalisation que le backend : espaces multiples et retours à la ligne réduits. */
export function normalizeText(value: string): string {
  return value.replace(/\s+/g, " ").trim();
}

export function validateHeroDraft(draft: HeroDraft): HeroFieldErrors {
  const errors: HeroFieldErrors = {};
  const name = normalizeText(draft.name);

  if (!name) {
    errors.name = "Indiquez le prénom du héros.";
  } else if (name.length > HERO_LIMITS.maxNameLength) {
    errors.name = `${HERO_LIMITS.maxNameLength} caractères au maximum.`;
  }

  if (draft.age === null) {
    errors.age = "Choisissez l'âge pour adapter le vocabulaire.";
  } else if (draft.age < HERO_LIMITS.minAge || draft.age > HERO_LIMITS.maxAge) {
    errors.age = `L'âge doit être compris entre ${HERO_LIMITS.minAge} et ${HERO_LIMITS.maxAge} ans.`;
  }

  const companions = draft.companions.map(normalizeText).filter(Boolean);
  if (companions.length > HERO_LIMITS.maxCompanions) {
    errors.companions = `${HERO_LIMITS.maxCompanions} compagnons au maximum.`;
  } else if (companions.some((c) => c.length > HERO_LIMITS.maxCompanionLength)) {
    errors.companions = `Chaque compagnon : ${HERO_LIMITS.maxCompanionLength} caractères au maximum.`;
  }

  for (const field of ["interests", "personality", "appearance"] as const) {
    if (normalizeText(draft[field]).length > HERO_LIMITS.maxDetailLength) {
      errors[field] = `${HERO_LIMITS.maxDetailLength} caractères au maximum.`;
    }
  }

  return errors;
}

export function isHeroDraftValid(draft: HeroDraft): boolean {
  return Object.keys(validateHeroDraft(draft)).length === 0;
}

/** Héros prêt à envoyer : texte normalisé, champs facultatifs vides omis. */
export function toHeroInput(draft: HeroDraft): HeroInput {
  const hero: HeroInput = { name: normalizeText(draft.name), age: draft.age ?? 0 };
  const companions = draft.companions.map(normalizeText).filter(Boolean);
  if (companions.length > 0) {
    hero.companions = companions;
  }

  for (const field of ["interests", "personality", "appearance"] as const) {
    const value = normalizeText(draft[field]);
    if (value) {
      hero[field] = value;
    }
  }

  return hero;
}

/** Résumé court (« Léa, 6 ans ») utilisé dans les récapitulatifs. */
export function describeHero(name: string, age: number): string {
  return `${name}, ${age} ans`;
}
