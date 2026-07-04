import { useQuery } from '@tanstack/react-query';
import { getThemes } from '../api/apiClient';

/** Récupère les thèmes disponibles pour démarrer une aventure. */
export function useThemes() {
  return useQuery({
    queryKey: ['themes'],
    queryFn: getThemes,
  });
}
