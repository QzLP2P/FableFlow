import type { ReaderMode } from "../api/types";
import { HERO_LIMITS } from "../domain/hero";
import {
  LocalDataError,
  createId,
  describeStorageFailure,
  isBoundedString,
  isRecord,
} from "./localData";

/**
 * Profils de la famille, conservés uniquement sur cet appareil (localStorage : petits
 * enregistrements). Le format est versionné et revalidé à chaque lecture.
 */
export const PROFILES_STORAGE_KEY = "fableflow:profiles";
export const PROFILES_SCHEMA_VERSION = 1;
export const MAX_PROFILES = 20;

export interface StoredProfile {
  id: string;
  name: string;
  age: number;
  companions: string[];
  interests: string;
  personality: string;
  appearance: string;
  readerMode: ReaderMode;
  readingAids: boolean;
  updatedAt: string;
}

export type ProfileInput = Omit<StoredProfile, "id" | "updatedAt"> & { id?: string };

export interface ProfilesSnapshot {
  profiles: StoredProfile[];
  /** Enregistrements illisibles ignorés (format inconnu ou corrompu). */
  invalidCount: number;
}

function getStorage(storage?: Storage): Storage {
  try {
    return storage ?? window.localStorage;
  } catch (error) {
    throw new LocalDataError(describeStorageFailure(error), { cause: error });
  }
}

export function parseProfile(value: unknown): StoredProfile | null {
  if (!isRecord(value)) {
    return null;
  }

  const { id, name, age, companions, interests, personality, appearance, readerMode, readingAids, updatedAt } = value;
  const valid =
    isBoundedString(id, 100) &&
    isBoundedString(name, HERO_LIMITS.maxNameLength) &&
    typeof age === "number" &&
    Number.isInteger(age) &&
    age >= HERO_LIMITS.minAge &&
    age <= HERO_LIMITS.maxAge &&
    Array.isArray(companions) &&
    companions.length <= HERO_LIMITS.maxCompanions &&
    companions.every((c) => isBoundedString(c, HERO_LIMITS.maxCompanionLength)) &&
    isBoundedString(interests, HERO_LIMITS.maxDetailLength, true) &&
    isBoundedString(personality, HERO_LIMITS.maxDetailLength, true) &&
    isBoundedString(appearance, HERO_LIMITS.maxDetailLength, true) &&
    (readerMode === "AdultReadAloud" || readerMode === "ChildReader") &&
    typeof readingAids === "boolean" &&
    isBoundedString(updatedAt, 40);

  if (!valid) {
    return null;
  }

  return {
    id,
    name,
    age,
    companions: [...(companions as string[])],
    interests,
    personality,
    appearance,
    readerMode,
    readingAids,
    updatedAt,
  };
}

export function loadProfiles(storage?: Storage): ProfilesSnapshot {
  let raw: string | null;
  try {
    raw = getStorage(storage).getItem(PROFILES_STORAGE_KEY);
  } catch (error) {
    throw new LocalDataError(describeStorageFailure(error), { cause: error });
  }

  if (raw === null) {
    return { profiles: [], invalidCount: 0 };
  }

  let envelope: unknown;
  try {
    envelope = JSON.parse(raw);
  } catch {
    return { profiles: [], invalidCount: 1 };
  }

  if (!isRecord(envelope) || envelope.version !== PROFILES_SCHEMA_VERSION || !Array.isArray(envelope.profiles)) {
    return { profiles: [], invalidCount: 1 };
  }

  const profiles: StoredProfile[] = [];
  let invalidCount = 0;
  for (const entry of envelope.profiles) {
    const profile = parseProfile(entry);
    if (profile) {
      profiles.push(profile);
    } else {
      invalidCount++;
    }
  }

  profiles.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  return { profiles, invalidCount };
}

function writeProfiles(profiles: StoredProfile[], storage?: Storage): void {
  try {
    getStorage(storage).setItem(
      PROFILES_STORAGE_KEY,
      JSON.stringify({ version: PROFILES_SCHEMA_VERSION, profiles }),
    );
  } catch (error) {
    throw new LocalDataError(
      `Le profil n'a pas pu être enregistré sur cet appareil. ${describeStorageFailure(error)}`,
      { cause: error },
    );
  }
}

/** Crée ou met à jour un profil (même identifiant) ; les profils illisibles sont écartés. */
export function saveProfile(input: ProfileInput, storage?: Storage, now: Date = new Date()): StoredProfile {
  const candidate = parseProfile({ ...input, id: input.id ?? createId(), updatedAt: now.toISOString() });
  if (!candidate) {
    throw new LocalDataError("Ce profil contient des informations invalides.");
  }

  const { profiles } = loadProfiles(storage);
  const others = profiles.filter((p) => p.id !== candidate.id);
  if (others.length >= MAX_PROFILES) {
    throw new LocalDataError(`Au plus ${MAX_PROFILES} profils peuvent être mémorisés. Supprimez-en un pour continuer.`);
  }

  writeProfiles([candidate, ...others], storage);
  return candidate;
}

export function deleteProfile(id: string, storage?: Storage): void {
  const { profiles } = loadProfiles(storage);
  writeProfiles(profiles.filter((p) => p.id !== id), storage);
}

/** Retire les profils illisibles en réécrivant uniquement les profils valides. */
export function discardInvalidProfiles(storage?: Storage): void {
  writeProfiles(loadProfiles(storage).profiles, storage);
}
