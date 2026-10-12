import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
import { advanceChapter, getAdventure, getAdventureHistory, isConflict, makeChoice, toApiError } from "../api/apiClient";
import type { AdventureDto } from "../api/types";
import { hasPendingIllustration, illustrationSignature } from "../domain/reader";
import { bookFromHistory, bookStore } from "../storage/books";
import { LocalDataError } from "../storage/localData";
import { getStoryReadingAids } from "../storage/readingPreferences";
import { libraryKey } from "./useLocalData";

const POLL_INTERVAL_MS = 5_000;
/** Durée maximale d'attente d'une même étape (illustration ou écriture en cours ailleurs). */
const POLL_WINDOW_MS = 120_000;

export const adventureKey = (adventureId: string) => ["adventure", adventureId] as const;

export function useAdventure(adventureId: string) {
  const pollWindow = useRef<{ signature: string; startedAt: number } | null>(null);

  return useQuery({
    queryKey: adventureKey(adventureId),
    queryFn: () => getAdventure(adventureId),
    retry: (failureCount, error) => toApiError(error).status === null && failureCount < 2,
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data || !(hasPendingIllustration(data) || data.isGenerating)) {
        pollWindow.current = null;
        return false;
      }

      const signature = `${data.chapters.length}:${illustrationSignature(data.chapters)}:${data.isGenerating}`;
      const now = Date.now();
      if (pollWindow.current?.signature !== signature) {
        pollWindow.current = { signature, startedAt: now };
      }
      return now - pollWindow.current.startedAt < POLL_WINDOW_MS ? POLL_INTERVAL_MS : false;
    },
  });
}

export type StoryAction =
  | { type: "advance"; fromChapterNumber: number }
  | { type: "choose"; chapterNumber: number; choiceId: string };

/** Avancer ou choisir : une seule action à la fois, rejouable telle quelle après un échec. */
export function useStoryAction(adventureId: string) {
  const queryClient = useQueryClient();
  const key = adventureKey(adventureId);

  return useMutation({
    mutationFn: (action: StoryAction) =>
      action.type === "advance"
        ? advanceChapter(adventureId, { fromChapterNumber: action.fromChapterNumber })
        : makeChoice(adventureId, { chapterNumber: action.chapterNumber, choiceId: action.choiceId }),
    onMutate: () => queryClient.cancelQueries({ queryKey: key }),
    onSuccess: (adventure: AdventureDto) => queryClient.setQueryData(key, adventure),
    onError: (error) => {
      if (isConflict(error)) {
        void queryClient.invalidateQueries({ queryKey: key });
      }
    },
  });
}

/**
 * Enregistre automatiquement l'histoire terminée dans la bibliothèque de l'appareil (instantané
 * de l'historique serveur). Réenregistrée si une illustration devient prête entre-temps.
 */
export function useAutoSaveBook(adventure: AdventureDto | undefined, themeName: string | null, ready: boolean) {
  const queryClient = useQueryClient();
  const adventureId = adventure?.adventureId ?? "";
  const signature = adventure ? illustrationSignature(adventure.chapters) : "";

  return useQuery({
    queryKey: ["save-book", adventureId, signature, themeName],
    queryFn: async () => {
      const history = await getAdventureHistory(adventureId);
      const book = bookFromHistory(history, {
        themeName,
        readingAids: getStoryReadingAids(adventureId) ?? false,
      });
      if (!book) {
        throw new LocalDataError("L'histoire n'est pas encore terminée.");
      }

      await bookStore.put(book);
      await queryClient.invalidateQueries({ queryKey: libraryKey });
      return book;
    },
    enabled: ready && adventure?.status === "Completed",
    staleTime: Infinity,
    retry: false,
  });
}

export function describeSaveError(error: unknown): string {
  return error instanceof LocalDataError ? error.message : toApiError(error).message;
}

/** Répercute la préférence d'aides sur le livre déjà enregistré (sans gravité en cas d'échec). */
export async function syncSavedBookReadingAids(bookId: string, readingAids: boolean): Promise<void> {
  try {
    const book = await bookStore.get(bookId);
    if (book && book.readingAids !== readingAids) {
      await bookStore.put({ ...book, readingAids });
    }
  } catch {
    // Préférence d'affichage uniquement.
  }
}
