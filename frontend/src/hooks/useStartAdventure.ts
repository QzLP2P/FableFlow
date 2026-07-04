import { useMutation } from '@tanstack/react-query';
import { startAdventure } from '../api/apiClient';
import type { StartAdventureRequest } from '../api/types';

/** Démarre une nouvelle aventure et génère la scène initiale. */
export function useStartAdventure() {
  return useMutation({
    mutationFn: (request: StartAdventureRequest) => startAdventure(request),
  });
}
