import { describe, expect, it } from "vitest";
import type { AdventureDto, ChapterDto, ChapterKind } from "../api/types";
import { frontierAction, illustrationSignature, resolveViewedChapterNumber, selectedChoiceLabel } from "./reader";

function chapter(number: number, kind: ChapterKind, overrides: Partial<ChapterDto> = {}): ChapterDto {
  return {
    number,
    kind,
    text: `Chapitre ${number}`,
    choices: [],
    selectedChoiceId: null,
    illustration: { status: "NotRequested", url: null },
    ...overrides,
  };
}

function adventure(chapters: ChapterDto[], overrides: Partial<AdventureDto> = {}): AdventureDto {
  return {
    adventureId: "a1",
    status: "InProgress",
    title: null,
    themeId: "pokemon",
    premise: "Une idée",
    length: "Short",
    readerMode: "AdultReadAloud",
    hero: { name: "Léa", age: 6, companions: [], interests: null, personality: null, appearance: null },
    totalChapters: 6,
    decisionChapters: [2, 4],
    currentChapterNumber: chapters.length,
    isGenerating: false,
    imageGenerationEnabled: false,
    chapters,
    ...overrides,
  };
}

describe("resolveViewedChapterNumber", () => {
  const chapters = [{ number: 1 }, { number: 2 }, { number: 3 }];

  it("shows the latest written chapter by default", () => {
    expect(resolveViewedChapterNumber(chapters, null)).toBe(3);
  });

  it("keeps an earlier chapter the reader went back to", () => {
    expect(resolveViewedChapterNumber(chapters, 1)).toBe(1);
  });

  it("falls back to the latest chapter for an unknown number", () => {
    expect(resolveViewedChapterNumber(chapters, 9)).toBe(3);
    expect(resolveViewedChapterNumber([], null)).toBe(1);
  });
});

describe("frontierAction", () => {
  it("advances after a narrative chapter", () => {
    expect(frontierAction(adventure([chapter(1, "Narrative")]))).toBe("advance");
  });

  it("asks for a choice at a decision chapter", () => {
    expect(frontierAction(adventure([chapter(1, "Narrative"), chapter(2, "Decision")]))).toBe("choose");
  });

  it("waits while the next chapter is being written", () => {
    expect(frontierAction(adventure([chapter(1, "Narrative")], { isGenerating: true }))).toBe("waiting");
  });

  it("finishes at the ending or once completed", () => {
    expect(frontierAction(adventure([chapter(1, "Ending")]))).toBe("finished");
    expect(frontierAction(adventure([chapter(1, "Narrative")], { status: "Completed" }))).toBe("finished");
  });
});

describe("chapter helpers", () => {
  it("returns the label of the selected choice", () => {
    const decision = chapter(2, "Decision", {
      choices: [
        { id: "a", label: "Ouvrir la porte" },
        { id: "b", label: "Appeler Biscuit" },
      ],
      selectedChoiceId: "b",
    });
    expect(selectedChoiceLabel(decision)).toBe("Appeler Biscuit");
    expect(selectedChoiceLabel(chapter(1, "Narrative"))).toBeNull();
  });

  it("changes the illustration signature when a status changes", () => {
    const pending = [chapter(1, "Narrative", { illustration: { status: "Pending", url: null } })];
    const ready = [chapter(1, "Narrative", { illustration: { status: "Ready", url: "https://img" } })];
    expect(illustrationSignature(pending)).not.toBe(illustrationSignature(ready));
  });
});
