import { useQuery } from '@tanstack/react-query';
import { getAdventure } from '../api/apiClient';

/** Récupère l'état courant d'une aventure. */
export function useAdventure(adventureId: string | undefined) {
  return useQuery({
    queryKey: ['adventure', adventureId],
    queryFn: () => getAdventure(adventureId!),
    enabled: Boolean(adventureId),
  });
}
