import { fireEvent, screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "../api/apiClient";
import type { AdventureDto } from "../api/types";
import { loadProfiles } from "../storage/profiles";
import { getStoryReadingAids } from "../storage/readingPreferences";
import { renderWithProviders } from "../test/renderWithProviders";
import { CreatePage } from "./CreatePage";

vi.mock("../api/apiClient", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../api/apiClient")>();
  return { ...actual, getThemes: vi.fn(), getStoryPremises: vi.fn(), startAdventure: vi.fn() };
});

const adventure: AdventureDto = {
  adventureId: "adv-1",
  status: "InProgress",
  title: "La forêt qui chante",
  themeId: "pokemon",
  premise: "Une mélodie guide Léa.",
  length: "Short",
  readerMode: "ChildReader",
  hero: { name: "Léa", age: 6, companions: [], interests: null, personality: null, appearance: null },
  totalChapters: 6,
  decisionChapters: [2, 4],
  currentChapterNumber: 1,
  isGenerating: false,
  imageGenerationEnabled: false,
  chapters: [],
};

function renderCreator() {
  return renderWithProviders(
    <Routes>
      <Route path="/create" element={<CreatePage />} />
      <Route path="/adventures/:adventureId" element={<p>Lecture en cours</p>} />
    </Routes>,
    { route: "/create" },
  );
}

describe("CreatePage", () => {
  beforeEach(() => {
    vi.mocked(api.getThemes).mockResolvedValue([
      { id: "pokemon", displayName: "Pokémon — Apprenti dresseur", description: "Des créatures à rencontrer." },
      { id: "dinosaur", displayName: "Dinosaures — Vallée préhistorique", description: "Une vallée oubliée." },
    ]);
    vi.mocked(api.getStoryPremises).mockResolvedValue([
      { title: "L'œuf qui brille", hook: "Léa trouve un œuf lumineux." },
      { title: "La forêt qui chante", hook: "Une mélodie guide Léa." },
    ]);
    vi.mocked(api.startAdventure).mockResolvedValue(adventure);
  });

  it("guides from hero to universe to premise and starts the chosen story", async () => {
    renderCreator();

    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));
    expect(screen.getByText("Indiquez le prénom du héros.")).toBeDefined();

    fireEvent.change(screen.getByLabelText(/Prénom du héros/), { target: { value: "Léa" } });
    fireEvent.click(screen.getByRole("button", { name: "6 ans" }));
    fireEvent.click(screen.getByRole("radio", { name: /L'enfant, tout seul/ }));
    expect(screen.getByRole("checkbox", { name: "Aides à la lecture" })).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));

    expect(await screen.findByRole("heading", { name: "Dans quel univers ?" })).toBeDefined();
    fireEvent.click(await screen.findByRole("radio", { name: /Pokémon/ }));

    fireEvent.click(screen.getByRole("button", { name: "Retour" }));
    expect((screen.getByLabelText(/Prénom du héros/) as HTMLInputElement).value).toBe("Léa");
    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));
    expect((await screen.findByRole("radio", { name: /Pokémon/ }) as HTMLInputElement).checked).toBe(true);

    fireEvent.click(screen.getByRole("radio", { name: /Plus longue/ }));
    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));

    const firstIdea = await screen.findByRole("radio", { name: /L'œuf qui brille/ });
    expect((firstIdea as HTMLInputElement).checked).toBe(true);
    expect(api.getStoryPremises).toHaveBeenCalledWith("pokemon", {
      length: "Longer",
      readerMode: "ChildReader",
      hero: { name: "Léa", age: 6 },
      count: 3,
    });
    expect(api.startAdventure).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("radio", { name: /La forêt qui chante/ }));
    fireEvent.click(screen.getByRole("button", { name: "Commencer l'histoire" }));

    expect(await screen.findByText("Lecture en cours")).toBeDefined();
    expect(api.startAdventure).toHaveBeenCalledWith({
      themeId: "pokemon",
      length: "Longer",
      readerMode: "ChildReader",
      hero: { name: "Léa", age: 6 },
      premise: "Une mélodie guide Léa.",
      premiseTitle: "La forêt qui chante",
    });
    expect(getStoryReadingAids("adv-1")).toBe(true);
    expect(loadProfiles().profiles).toEqual([
      expect.objectContaining({ name: "Léa", age: 6, readerMode: "ChildReader", readingAids: true }),
    ]);
  });

  it("keeps the choices and offers a custom idea when ideas cannot be generated", async () => {
    vi.mocked(api.getStoryPremises).mockRejectedValue(new Error("boom"));
    renderCreator();

    fireEvent.change(screen.getByLabelText(/Prénom du héros/), { target: { value: "Noé" } });
    fireEvent.click(screen.getByRole("button", { name: "8 ans" }));
    fireEvent.click(screen.getByRole("checkbox", { name: /Mémoriser ce profil/ }));
    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));
    fireEvent.click(await screen.findByRole("radio", { name: /Dinosaures/ }));
    fireEvent.click(screen.getByRole("button", { name: "Continuer" }));

    expect(await screen.findByText(/Vous pouvez aussi écrire votre propre idée/)).toBeDefined();
    const start = screen.getByRole("button", { name: "Commencer l'histoire" }) as HTMLButtonElement;
    expect(start.disabled).toBe(true);

    fireEvent.click(screen.getByRole("radio", { name: /Ma propre idée/ }));
    fireEvent.click(screen.getByRole("button", { name: "Commencer l'histoire" }));
    expect(screen.getByText("Décrivez votre idée en une ou deux phrases.")).toBeDefined();

    fireEvent.change(screen.getByLabelText(/Votre idée/), { target: { value: "Un volcan  qui chante" } });
    fireEvent.click(screen.getByRole("button", { name: "Commencer l'histoire" }));

    await waitFor(() =>
      expect(api.startAdventure).toHaveBeenCalledWith(
        expect.objectContaining({ themeId: "dinosaur", hero: { name: "Noé", age: 8 }, premise: "Un volcan qui chante" }),
      ),
    );
    expect(loadProfiles().profiles).toEqual([]);
  });
});
