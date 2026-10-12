import { describe, expect, it } from "vitest";
import type { StoryPremiseDto } from "../../api/types";
import type { StoredProfile } from "../../storage/profiles";
import {
  buildStartRequest,
  creatorReducer,
  effectiveReadingAids,
  initialCreatorState,
  premiseRequest,
  type CreatorAction,
  type CreatorState,
} from "./creatorState";

const premises: StoryPremiseDto[] = [
  { title: "L'œuf qui brille", hook: "Léa trouve un œuf lumineux." },
  { title: "La forêt qui chante", hook: "Une mélodie guide Léa." },
  { title: "Le pont des nuages", hook: "Un pont apparaît dans le ciel." },
];

const profile: StoredProfile = {
  id: "p1",
  name: "Noé",
  age: 8,
  companions: ["Biscuit"],
  interests: "les volcans",
  personality: "",
  appearance: "",
  readerMode: "ChildReader",
  readingAids: false,
  updatedAt: "2026-01-01T00:00:00.000Z",
};

function apply(state: CreatorState, ...actions: CreatorAction[]): CreatorState {
  return actions.reduce(creatorReducer, state);
}

function atPremiseStep(): CreatorState {
  return apply(
    initialCreatorState,
    { type: "updateHero", patch: { name: "  Léa ", age: 6 } },
    { type: "next" },
    { type: "selectTheme", themeId: "pokemon" },
    { type: "next" },
  );
}

describe("creatorReducer", () => {
  it("starts with an adult read-aloud short story and no theme", () => {
    expect(initialCreatorState).toMatchObject({
      step: 0,
      readerMode: "AdultReadAloud",
      length: "Short",
      themeId: null,
      rememberProfile: true,
      premiseSelection: { kind: "suggestion", index: 0 },
    });
  });

  it("blocks the hero step until name and age are valid", () => {
    const blocked = apply(initialCreatorState, { type: "updateHero", patch: { name: "Léa" } }, { type: "next" });
    expect(blocked.step).toBe(0);
    expect(blocked.showHeroErrors).toBe(true);

    const passed = apply(blocked, { type: "updateHero", patch: { age: 6 } }, { type: "next" });
    expect(passed.step).toBe(1);
    expect(passed.showHeroErrors).toBe(false);
  });

  it("requires a theme before the premise step", () => {
    const state = apply(initialCreatorState, { type: "updateHero", patch: { name: "Léa", age: 6 } }, { type: "next" }, { type: "next" });
    expect(state.step).toBe(1);
    expect(state.showThemeError).toBe(true);

    const chosen = apply(state, { type: "selectTheme", themeId: "dinosaur" });
    expect(chosen.showThemeError).toBe(false);
    expect(apply(chosen, { type: "next" }).step).toBe(2);
  });

  it("keeps every entry when navigating back", () => {
    const state = apply(atPremiseStep(), { type: "setCustomPremise", value: "Un dragon timide" }, { type: "back" }, { type: "back" });
    expect(state.step).toBe(0);
    expect(state.hero.name).toBe("  Léa ");
    expect(state.themeId).toBe("pokemon");
    expect(state.customPremise).toBe("Un dragon timide");
    expect(state.premiseSelection).toEqual({ kind: "custom" });
  });

  it("only jumps backwards through the stepper", () => {
    const state = atPremiseStep();
    expect(apply(state, { type: "goToStep", step: 0 }).step).toBe(0);
    expect(apply(state, { type: "goToStep", step: 0 }, { type: "goToStep", step: 2 }).step).toBe(0);
  });

  it.each<CreatorAction>([
    { type: "updateHero", patch: { name: "Lou" } },
    { type: "selectTheme", themeId: "vet" },
    { type: "setLength", value: "Longer" },
    { type: "setReaderMode", value: "ChildReader" },
  ])("starts over from the first fresh suggestion after $type", (action) => {
    const state = apply(atPremiseStep(), { type: "morePremises" }, { type: "selectPremise", selection: { kind: "suggestion", index: 2 } }, action);
    expect(state.premiseGeneration).toBe(0);
    expect(state.premiseSelection).toEqual({ kind: "suggestion", index: 0 });
  });

  it("keeps a custom idea selected when personalization changes", () => {
    const state = apply(atPremiseStep(), { type: "setCustomPremise", value: "Mon idée" }, { type: "setLength", value: "Longer" });
    expect(state.premiseSelection).toEqual({ kind: "custom" });
    expect(state.customPremise).toBe("Mon idée");
  });

  it("asks for other ideas with a new generation and preselects the first", () => {
    const state = apply(atPremiseStep(), { type: "selectPremise", selection: { kind: "suggestion", index: 1 } }, { type: "morePremises" });
    expect(state.premiseGeneration).toBe(1);
    expect(state.premiseSelection).toEqual({ kind: "suggestion", index: 0 });
  });

  it("does not reset suggestions when re-selecting the same theme, length or reader", () => {
    const base = apply(atPremiseStep(), { type: "morePremises" });
    const same = apply(
      base,
      { type: "selectTheme", themeId: "pokemon" },
      { type: "setLength", value: "Short" },
      { type: "setReaderMode", value: "AdultReadAloud" },
    );
    expect(same.premiseGeneration).toBe(1);
  });

  it("fills hero and reading defaults from a saved profile, and clears them for a new hero", () => {
    const loaded = apply(initialCreatorState, { type: "selectProfile", profile });
    expect(loaded.profileId).toBe("p1");
    expect(loaded.hero).toMatchObject({ name: "Noé", age: 8, companions: ["Biscuit"], interests: "les volcans" });
    expect(loaded.readerMode).toBe("ChildReader");
    expect(loaded.readingAids).toBe(false);

    const cleared = apply(loaded, { type: "newHero" });
    expect(cleared.profileId).toBeNull();
    expect(cleared.hero.name).toBe("");
  });
});

describe("creator requests", () => {
  it("sends the normalized hero snapshot without empty optional fields", () => {
    const state = apply(atPremiseStep(), { type: "updateHero", patch: { companions: [" Biscuit ", ""], interests: " " } });
    expect(premiseRequest(state)).toEqual({
      length: "Short",
      readerMode: "AdultReadAloud",
      hero: { name: "Léa", age: 6, companions: ["Biscuit"] },
      count: 3,
    });
  });

  it("starts from the selected suggestion with its title", () => {
    const state = apply(atPremiseStep(), { type: "selectPremise", selection: { kind: "suggestion", index: 1 } });
    expect(buildStartRequest(state, premises)).toEqual({
      themeId: "pokemon",
      length: "Short",
      readerMode: "AdultReadAloud",
      hero: { name: "Léa", age: 6 },
      premise: "Une mélodie guide Léa.",
      premiseTitle: "La forêt qui chante",
    });
  });

  it("starts from a normalized custom idea without a title", () => {
    const state = apply(atPremiseStep(), { type: "setCustomPremise", value: "  Un dragon\n timide " });
    const request = buildStartRequest(state, premises);
    expect(request?.premise).toBe("Un dragon timide");
    expect(request).not.toHaveProperty("premiseTitle");
  });

  it("refuses to start without a usable idea", () => {
    expect(buildStartRequest(atPremiseStep(), undefined)).toBeNull();
    expect(buildStartRequest(apply(atPremiseStep(), { type: "setCustomPremise", value: "   " }), premises)).toBeNull();
    expect(buildStartRequest(apply(atPremiseStep(), { type: "setCustomPremise", value: "x".repeat(601) }), premises)).toBeNull();
  });

  it("only applies reading aids to a child reader", () => {
    expect(effectiveReadingAids(apply(initialCreatorState, { type: "setReadingAids", value: true }))).toBe(false);
    expect(effectiveReadingAids(apply(initialCreatorState, { type: "setReaderMode", value: "ChildReader" }))).toBe(true);
    expect(
      effectiveReadingAids(apply(initialCreatorState, { type: "setReaderMode", value: "ChildReader" }, { type: "setReadingAids", value: false })),
    ).toBe(false);
  });
});
