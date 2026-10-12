import type { ReaderMode, StartAdventureRequest, StoryLength, StoryPremiseDto, StoryPremisesRequest } from "../../api/types";
import {
  emptyHeroDraft,
  isHeroDraftValid,
  MAX_PREMISE_LENGTH,
  normalizeText,
  toHeroInput,
  type HeroDraft,
} from "../../domain/hero";
import type { StoredProfile } from "../../storage/profiles";

export type CreatorStep = 0 | 1 | 2;
export type PremiseSelection = { kind: "suggestion"; index: number } | { kind: "custom" };

export const PREMISE_COUNT = 3;

/** État complet de l'assistant : conservé en revenant en arrière, jamais perdu entre étapes. */
export interface CreatorState {
  step: CreatorStep;
  hero: HeroDraft;
  profileId: string | null;
  rememberProfile: boolean;
  readerMode: ReaderMode;
  readingAids: boolean;
  themeId: string | null;
  length: StoryLength;
  premiseGeneration: number;
  premiseSelection: PremiseSelection;
  customPremise: string;
  showHeroErrors: boolean;
  showThemeError: boolean;
}

export const initialCreatorState: CreatorState = {
  step: 0,
  hero: emptyHeroDraft,
  profileId: null,
  rememberProfile: true,
  readerMode: "AdultReadAloud",
  readingAids: true,
  themeId: null,
  length: "Short",
  premiseGeneration: 0,
  premiseSelection: { kind: "suggestion", index: 0 },
  customPremise: "",
  showHeroErrors: false,
  showThemeError: false,
};

export type CreatorAction =
  | { type: "updateHero"; patch: Partial<HeroDraft> }
  | { type: "selectProfile"; profile: StoredProfile }
  | { type: "newHero" }
  | { type: "profileSaved"; profileId: string }
  | { type: "setRememberProfile"; value: boolean }
  | { type: "setReaderMode"; value: ReaderMode }
  | { type: "setReadingAids"; value: boolean }
  | { type: "selectTheme"; themeId: string }
  | { type: "setLength"; value: StoryLength }
  | { type: "next" }
  | { type: "back" }
  | { type: "goToStep"; step: CreatorStep }
  | { type: "morePremises" }
  | { type: "selectPremise"; selection: PremiseSelection }
  | { type: "setCustomPremise"; value: string };

/** Les idées dépendent du héros, du lecteur, de l'univers et de la longueur : on repart de la première. */
function withFreshPremises(state: CreatorState): CreatorState {
  return {
    ...state,
    premiseGeneration: 0,
    premiseSelection: state.premiseSelection.kind === "custom" ? state.premiseSelection : { kind: "suggestion", index: 0 },
  };
}

export function heroDraftFromProfile(profile: StoredProfile): HeroDraft {
  return {
    name: profile.name,
    age: profile.age,
    companions: [...profile.companions],
    interests: profile.interests,
    personality: profile.personality,
    appearance: profile.appearance,
  };
}

export function creatorReducer(state: CreatorState, action: CreatorAction): CreatorState {
  switch (action.type) {
    case "updateHero":
      return withFreshPremises({ ...state, hero: { ...state.hero, ...action.patch } });
    case "selectProfile":
      return withFreshPremises({
        ...state,
        hero: heroDraftFromProfile(action.profile),
        profileId: action.profile.id,
        readerMode: action.profile.readerMode,
        readingAids: action.profile.readingAids,
        showHeroErrors: false,
      });
    case "newHero":
      return withFreshPremises({ ...state, hero: emptyHeroDraft, profileId: null, showHeroErrors: false });
    case "profileSaved":
      return { ...state, profileId: action.profileId };
    case "setRememberProfile":
      return { ...state, rememberProfile: action.value };
    case "setReaderMode":
      return action.value === state.readerMode ? state : withFreshPremises({ ...state, readerMode: action.value });
    case "setReadingAids":
      return { ...state, readingAids: action.value };
    case "selectTheme":
      return action.themeId === state.themeId
        ? { ...state, showThemeError: false }
        : withFreshPremises({ ...state, themeId: action.themeId, showThemeError: false });
    case "setLength":
      return action.value === state.length ? state : withFreshPremises({ ...state, length: action.value });
    case "next":
      if (state.step === 0) {
        return isHeroDraftValid(state.hero) ? { ...state, step: 1, showHeroErrors: false } : { ...state, showHeroErrors: true };
      }
      if (state.step === 1) {
        return state.themeId ? { ...state, step: 2, showThemeError: false } : { ...state, showThemeError: true };
      }
      return state;
    case "back":
      return state.step === 0 ? state : { ...state, step: (state.step - 1) as CreatorStep };
    case "goToStep":
      return action.step < state.step ? { ...state, step: action.step } : state;
    case "morePremises":
      return {
        ...state,
        premiseGeneration: state.premiseGeneration + 1,
        premiseSelection: { kind: "suggestion", index: 0 },
      };
    case "selectPremise":
      return { ...state, premiseSelection: action.selection };
    case "setCustomPremise":
      return { ...state, customPremise: action.value, premiseSelection: { kind: "custom" } };
  }
}

export function premiseRequest(state: CreatorState): StoryPremisesRequest {
  return {
    length: state.length,
    readerMode: state.readerMode,
    hero: toHeroInput(state.hero),
    count: PREMISE_COUNT,
  };
}

/** Message d'erreur de l'idée personnalisée, ou `null` si elle est utilisable. */
export function customPremiseError(value: string): string | null {
  const premise = normalizeText(value);
  if (!premise) {
    return "Décrivez votre idée en une ou deux phrases.";
  }
  return premise.length > MAX_PREMISE_LENGTH ? `${MAX_PREMISE_LENGTH} caractères au maximum.` : null;
}

/** Requête de démarrage, ou `null` tant qu'aucune idée valide n'est choisie. */
export function buildStartRequest(
  state: CreatorState,
  premises: readonly StoryPremiseDto[] | undefined,
): StartAdventureRequest | null {
  if (!state.themeId || !isHeroDraftValid(state.hero)) {
    return null;
  }

  const base = {
    themeId: state.themeId,
    length: state.length,
    readerMode: state.readerMode,
    hero: toHeroInput(state.hero),
  };

  if (state.premiseSelection.kind === "custom") {
    return customPremiseError(state.customPremise) ? null : { ...base, premise: normalizeText(state.customPremise) };
  }

  const suggestion = premises?.[state.premiseSelection.index];
  return suggestion ? { ...base, premise: suggestion.hook, premiseTitle: suggestion.title } : null;
}

/** Les aides ne s'appliquent qu'à l'enfant lecteur. */
export function effectiveReadingAids(state: CreatorState): boolean {
  return state.readerMode === "ChildReader" && state.readingAids;
}
