import { useQuery } from "@tanstack/react-query";
import { getStoryPremises, getThemes } from "../api/apiClient";
import type { StoryPremisesRequest } from "../api/types";

export function useThemes() {
  return useQuery({
    queryKey: ["themes"],
    queryFn: getThemes,
    staleTime: Infinity,
  });
}

/**
 * Idées d'histoire pour un héros et un univers. `generation` permet de redemander d'autres idées
 * sans invalider les précédentes ; aucune relance automatique (appel LLM coûteux).
 */
export function useStoryPremises(
  themeId: string | null,
  request: StoryPremisesRequest,
  generation: number,
  enabled: boolean,
) {
  return useQuery({
    queryKey: ["story-premises", themeId, request.length, request.readerMode, request.hero, generation],
    queryFn: () => getStoryPremises(themeId ?? "", request),
    enabled: enabled && themeId !== null,
    staleTime: Infinity,
    retry: false,
  });
}
