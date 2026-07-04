import { useMutation, useQueryClient } from '@tanstack/react-query';
import { makeChoice } from '../api/apiClient';
import type { AdventureDto } from '../api/types';

/** Applique un choix utilisateur et récupère la scène suivante (ou l'issue finale). */
export function useMakeChoice(adventureId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (choiceId: string) => makeChoice(adventureId!, { choiceId }),
    onSuccess: (updated: AdventureDto) => {
      queryClient.setQueryData(['adventure', adventureId], updated);
    },
  });
}
