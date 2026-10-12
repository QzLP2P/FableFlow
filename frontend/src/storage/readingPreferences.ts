import { isRecord } from "./localData";

/**
 * Préférence d'affichage « Aides à la lecture » par histoire. Elle ne fait pas partie de l'état
 * narratif : la basculer ne modifie ni ne régénère jamais le texte. Une écriture impossible est
 * sans gravité (la bascule reste active pour la lecture en cours).
 */
const STORAGE_KEY = "fableflow:reading-aids";
const SCHEMA_VERSION = 1;
const MAX_ENTRIES = 50;

interface Entry {
  aids: boolean;
  at: number;
}

function read(): Record<string, Entry> {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : null;
    if (!isRecord(parsed) || parsed.version !== SCHEMA_VERSION || !isRecord(parsed.stories)) {
      return {};
    }

    const entries: Record<string, Entry> = {};
    for (const [id, value] of Object.entries(parsed.stories)) {
      if (isRecord(value) && typeof value.aids === "boolean" && typeof value.at === "number") {
        entries[id] = { aids: value.aids, at: value.at };
      }
    }
    return entries;
  } catch {
    return {};
  }
}

export function getStoryReadingAids(storyId: string): boolean | null {
  return read()[storyId]?.aids ?? null;
}

export function setStoryReadingAids(storyId: string, aids: boolean): void {
  const entries = { ...read(), [storyId]: { aids, at: Date.now() } };
  const kept = Object.entries(entries)
    .sort(([, a], [, b]) => b.at - a.at)
    .slice(0, MAX_ENTRIES);

  try {
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ version: SCHEMA_VERSION, stories: Object.fromEntries(kept) }),
    );
  } catch {
    // Préférence de présentation uniquement : on garde l'état en mémoire.
  }
}
