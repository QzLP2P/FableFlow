/** Types alignés sur les DTOs exposés par FableFlow.Api (énumérations sérialisées par leur nom). */

export type StoryLength = "Short" | "Longer";
export type ReaderMode = "AdultReadAloud" | "ChildReader";
export type SessionStatus = "InProgress" | "Completed";
export type ChapterKind = "Narrative" | "Decision" | "Ending";
export type IllustrationStatus = "NotRequested" | "Pending" | "Ready" | "Failed";

export interface ThemeDto {
  id: string;
  displayName: string;
  description: string;
}

export interface StoryPremiseDto {
  title: string;
  hook: string;
}

/** Héros envoyé à l'API ; les champs facultatifs vides sont omis. */
export interface HeroInput {
  name: string;
  age: number;
  companions?: string[];
  interests?: string;
  personality?: string;
  appearance?: string;
}

export interface HeroDto {
  name: string;
  age: number;
  companions: string[];
  interests: string | null;
  personality: string | null;
  appearance: string | null;
}

export interface ChoiceDto {
  id: string;
  label: string;
}

export interface IllustrationDto {
  status: IllustrationStatus;
  url: string | null;
}

export interface ChapterDto {
  number: number;
  kind: ChapterKind;
  text: string;
  choices: ChoiceDto[];
  selectedChoiceId: string | null;
  illustration: IllustrationDto;
}

export interface AdventureDto {
  adventureId: string;
  status: SessionStatus;
  title: string | null;
  themeId: string;
  premise: string;
  length: StoryLength;
  readerMode: ReaderMode;
  hero: HeroDto;
  totalChapters: number;
  decisionChapters: number[];
  currentChapterNumber: number;
  isGenerating: boolean;
  imageGenerationEnabled: boolean;
  chapters: ChapterDto[];
}

export interface HistoryChapterDto {
  number: number;
  kind: ChapterKind;
  text: string;
  selectedChoiceLabel: string | null;
  illustration: IllustrationDto;
}

export interface AdventureHistoryDto {
  adventureId: string;
  status: SessionStatus;
  title: string | null;
  themeId: string;
  premise: string;
  length: StoryLength;
  readerMode: ReaderMode;
  hero: HeroDto;
  totalChapters: number;
  chapters: HistoryChapterDto[];
  conclusion: string | null;
}

export interface StoryPremisesRequest {
  length: StoryLength;
  readerMode: ReaderMode;
  hero: HeroInput;
  count?: number;
}

export interface StartAdventureRequest {
  themeId: string;
  length: StoryLength;
  readerMode: ReaderMode;
  hero: HeroInput;
  premise: string;
  premiseTitle?: string;
}

export interface AdvanceChapterRequest {
  fromChapterNumber: number;
}

export interface MakeChoiceRequest {
  chapterNumber: number;
  choiceId: string;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
