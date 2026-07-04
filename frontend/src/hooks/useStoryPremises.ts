import { useQuery } from "@tanstack/react-query";
import { getStoryPremises } from "../api/apiClient";

const DEFAULT_PREMISE_COUNT = 3;

/**
 * Récupère des propositions d'axe narratif pour un thème donné. Désactivée tant qu'aucun
 * thème n'est sélectionné. Le bouton "Autres idées" appelle `refetch` pour régénérer de
 * nouvelles propositions via le LLM (la clé de requête reste stable, `refetch` force l'appel).
 */
export function useStoryPremises(
  themeId: string | null,
  count = DEFAULT_PREMISE_COUNT,
) {
  return useQuery({
    queryKey: ["story-premises", themeId, count],
    queryFn: () => getStoryPremises(themeId as string, count),
    enabled: themeId !== null,
    staleTime: 0,
    gcTime: 0,
    retry: false,
  });
}
