import { useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getAdventure } from '../api/apiClient';
import type { AdventureDto } from '../api/types';

/** Intervalle de sondage (ms) tant qu'une illustration est encore en cours de génération. */
const IMAGE_POLL_INTERVAL_MS = 2500;

/** Délai maximum (ms) d'attente d'une illustration avant d'abandonner le sondage pour cette scène/issue. */
const IMAGE_POLL_TIMEOUT_MS = 45_000;

/**
 * Identifie le cycle d'attente d'image courant (scène en cours ou issue finale), ou `null` si
 * aucune image n'est attendue (génération désactivée, ou image déjà disponible).
 */
function getPendingImageKey(adventure: AdventureDto | undefined): string | null {
  if (!adventure || !adventure.imageGenerationEnabled) {
    return null;
  }

  if (adventure.status === 'InProgress') {
    return adventure.currentScene && !adventure.currentScene.imageUrl
      ? `scene:${adventure.currentScene.sceneNumber}`
      : null;
  }

  return adventure.outcomeImageUrl ? null : 'outcome';
}

/**
 * Récupère l'état courant d'une aventure. Le texte et les choix arrivent immédiatement ; tant que
 * l'illustration de la scène courante (ou de l'issue finale) n'est pas encore prête, l'aventure est
 * sondée régulièrement (polling) jusqu'à son arrivée ou l'expiration d'un délai raisonnable.
 */
export function useAdventure(adventureId: string | undefined) {
  const pollState = useRef<{ key: string; startedAt: number } | null>(null);

  return useQuery({
    queryKey: ['adventure', adventureId],
    queryFn: () => getAdventure(adventureId!),
    enabled: Boolean(adventureId),
    refetchInterval: (query) => {
      const key = getPendingImageKey(query.state.data);

      if (!key) {
        pollState.current = null;
        return false;
      }

      if (pollState.current?.key !== key) {
        pollState.current = { key, startedAt: Date.now() };
      }

      const elapsed = Date.now() - pollState.current.startedAt;
      return elapsed < IMAGE_POLL_TIMEOUT_MS ? IMAGE_POLL_INTERVAL_MS : false;
    },
  });
}
