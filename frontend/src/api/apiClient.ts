import axios from "axios";
import type {
  AdvanceChapterRequest,
  AdventureDto,
  AdventureHistoryDto,
  MakeChoiceRequest,
  ProblemDetails,
  StartAdventureRequest,
  StoryPremiseDto,
  StoryPremisesRequest,
  ThemeDto,
} from "./types";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

export const apiClient = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json",
  },
});

export async function getThemes(): Promise<ThemeDto[]> {
  const { data } = await apiClient.get<ThemeDto[]>("/api/themes");
  return data;
}

/** POST : les informations du héros ne transitent jamais dans l'URL. */
export async function getStoryPremises(
  themeId: string,
  request: StoryPremisesRequest,
): Promise<StoryPremiseDto[]> {
  const { data } = await apiClient.post<StoryPremiseDto[]>(
    `/api/themes/${encodeURIComponent(themeId)}/story-premises`,
    request,
  );
  return data;
}

export async function startAdventure(
  request: StartAdventureRequest,
): Promise<AdventureDto> {
  const { data } = await apiClient.post<AdventureDto>("/api/adventures", request);
  return data;
}

export async function getAdventure(adventureId: string): Promise<AdventureDto> {
  const { data } = await apiClient.get<AdventureDto>(
    `/api/adventures/${encodeURIComponent(adventureId)}`,
  );
  return data;
}

export async function advanceChapter(
  adventureId: string,
  request: AdvanceChapterRequest,
): Promise<AdventureDto> {
  const { data } = await apiClient.post<AdventureDto>(
    `/api/adventures/${encodeURIComponent(adventureId)}/advance`,
    request,
  );
  return data;
}

export async function makeChoice(
  adventureId: string,
  request: MakeChoiceRequest,
): Promise<AdventureDto> {
  const { data } = await apiClient.post<AdventureDto>(
    `/api/adventures/${encodeURIComponent(adventureId)}/choices`,
    request,
  );
  return data;
}

export async function getAdventureHistory(
  adventureId: string,
): Promise<AdventureHistoryDto> {
  const { data } = await apiClient.get<AdventureHistoryDto>(
    `/api/adventures/${encodeURIComponent(adventureId)}/history`,
  );
  return data;
}

/** Erreur d'API normalisée : statut HTTP éventuel et message affichable en français. */
export interface ApiError {
  status: number | null;
  message: string;
  fieldErrors: Record<string, string[]>;
}

const fallbackMessages: Record<number, string> = {
  400: "La demande n'est pas valide.",
  404: "Élément introuvable.",
  409: "L'histoire a changé entre-temps.",
  502: "L'écriture n'a pas abouti. Réessayez dans un instant.",
};

/** Convertit n'importe quelle erreur (réseau, ProblemDetails, inattendue) en message clair. */
export function toApiError(error: unknown): ApiError {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    if (!error.response) {
      return {
        status: null,
        message: "Le serveur ne répond pas. Vérifiez la connexion puis réessayez.",
        fieldErrors: {},
      };
    }

    const { status, data } = error.response;
    const title = typeof data?.title === "string" && data.title.trim() ? data.title : null;
    return {
      status,
      message: title ?? fallbackMessages[status] ?? "Une erreur inattendue est survenue.",
      fieldErrors: data?.errors ?? {},
    };
  }

  return { status: null, message: "Une erreur inattendue est survenue.", fieldErrors: {} };
}

export function isConflict(error: unknown): boolean {
  return toApiError(error).status === 409;
}
