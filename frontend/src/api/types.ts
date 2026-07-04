/** Types alignés sur les DTOs exposés par FableFlow.Api. */

export interface ThemeDto {
  id: string;
  displayName: string;
  audience: string;
  vocabularyLevel: string;
}

export interface StoryPremiseDto {
  title: string;
  hook: string;
}

export interface ChoiceDto {
  id: string;
  label: string;
}

export interface SceneDto {
  sceneNumber: number;
  text: string;
  imageUrl: string | null;
  choices: ChoiceDto[];
}

export type AdventureStatus = 'InProgress' | 'Won' | 'Lost' | 'Completed';

export interface AdventureDto {
  adventureId: string;
  status: AdventureStatus;
  currentSceneNumber: number;
  targetSceneCount: number;
  currentScene: SceneDto | null;
  outcomeMessage: string | null;
}

export interface AdventureHistoryDto {
  adventureId: string;
  status: AdventureStatus;
  scenes: SceneDto[];
}

export interface StartAdventureRequest {
  themeId: string;
  sceneCount: number;
  narrativePremise?: string;
}

export interface MakeChoiceRequest {
  choiceId: string;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
