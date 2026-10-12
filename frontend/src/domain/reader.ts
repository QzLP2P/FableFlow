import type { AdventureDto, ChapterDto, IllustrationDto } from "../api/types";

/** Numéro du chapitre affiché : celui demandé s'il existe, sinon le dernier chapitre écrit. */
export function resolveViewedChapterNumber(chapters: readonly { number: number }[], viewedNumber: number | null): number {
  const frontier = chapters.length > 0 ? chapters[chapters.length - 1].number : 1;
  if (viewedNumber === null || !chapters.some((chapter) => chapter.number === viewedNumber)) {
    return frontier;
  }
  return viewedNumber;
}

export function frontierChapter(adventure: AdventureDto): ChapterDto | null {
  return adventure.chapters[adventure.chapters.length - 1] ?? null;
}

/** Action attendue au dernier chapitre écrit. */
export type FrontierAction = "advance" | "choose" | "finished" | "waiting";

export function frontierAction(adventure: AdventureDto): FrontierAction {
  const frontier = frontierChapter(adventure);
  if (!frontier || adventure.status === "Completed" || frontier.kind === "Ending") {
    return "finished";
  }
  if (adventure.isGenerating) {
    return "waiting";
  }
  return frontier.kind === "Decision" ? "choose" : "advance";
}

/** Libellé du choix retenu à un chapitre de décision, ou `null`. */
export function selectedChoiceLabel(chapter: ChapterDto): string | null {
  return chapter.choices.find((choice) => choice.id === chapter.selectedChoiceId)?.label ?? null;
}

export function hasPendingIllustration(adventure: AdventureDto | undefined): boolean {
  return adventure?.chapters.some((chapter) => chapter.illustration.status === "Pending") ?? false;
}

/** Empreinte des statuts d'illustration : change quand une image devient prête ou échoue. */
export function illustrationSignature(chapters: readonly { illustration: IllustrationDto }[]): string {
  return chapters.map((chapter) => chapter.illustration.status[0]).join("");
}
