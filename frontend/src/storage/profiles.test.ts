import { describe, expect, it } from "vitest";
import { LocalDataError } from "./localData";
import {
  MAX_PROFILES,
  PROFILES_STORAGE_KEY,
  deleteProfile,
  discardInvalidProfiles,
  loadProfiles,
  saveProfile,
  type ProfileInput,
} from "./profiles";

function memoryStorage(initial: Record<string, string> = {}): Storage {
  const data = new Map(Object.entries(initial));
  return {
    get length() {
      return data.size;
    },
    clear: () => data.clear(),
    getItem: (key) => data.get(key) ?? null,
    key: (index) => [...data.keys()][index] ?? null,
    removeItem: (key) => void data.delete(key),
    setItem: (key, value) => void data.set(key, value),
  };
}

const lea: ProfileInput = {
  name: "Léa",
  age: 6,
  companions: ["Moustache"],
  interests: "les dinosaures",
  personality: "",
  appearance: "",
  readerMode: "ChildReader",
  readingAids: true,
};

describe("profiles storage", () => {
  it("enregistre, relit et met à jour un profil sans doublon", () => {
    const storage = memoryStorage();
    const saved = saveProfile(lea, storage, new Date("2025-01-01T10:00:00Z"));
    saveProfile({ ...lea, id: saved.id, age: 7 }, storage, new Date("2025-01-02T10:00:00Z"));

    const { profiles, invalidCount } = loadProfiles(storage);
    expect(invalidCount).toBe(0);
    expect(profiles).toHaveLength(1);
    expect(profiles[0]).toMatchObject({ id: saved.id, name: "Léa", age: 7, readingAids: true });
  });

  it("trie les profils du plus récent au plus ancien et les supprime", () => {
    const storage = memoryStorage();
    const first = saveProfile(lea, storage, new Date("2025-01-01T10:00:00Z"));
    saveProfile({ ...lea, name: "Tom" }, storage, new Date("2025-01-03T10:00:00Z"));

    expect(loadProfiles(storage).profiles.map((p) => p.name)).toEqual(["Tom", "Léa"]);
    deleteProfile(first.id, storage);
    expect(loadProfiles(storage).profiles.map((p) => p.name)).toEqual(["Tom"]);
  });

  it("ignore et compte les enregistrements illisibles puis les retire", () => {
    const valid = { ...lea, id: "a", updatedAt: "2025-01-01T00:00:00.000Z" };
    const storage = memoryStorage({
      [PROFILES_STORAGE_KEY]: JSON.stringify({
        version: 1,
        profiles: [valid, { ...valid, id: "b", age: 42 }, "texte", { id: "c" }],
      }),
    });

    expect(loadProfiles(storage)).toMatchObject({ invalidCount: 3, profiles: [{ id: "a" }] });
    discardInvalidProfiles(storage);
    expect(loadProfiles(storage).invalidCount).toBe(0);
  });

  it("considère un format inconnu ou corrompu comme illisible", () => {
    expect(loadProfiles(memoryStorage({ [PROFILES_STORAGE_KEY]: "{oops" })).invalidCount).toBe(1);
    expect(
      loadProfiles(memoryStorage({ [PROFILES_STORAGE_KEY]: JSON.stringify({ version: 99, profiles: [] }) })).invalidCount,
    ).toBe(1);
  });

  it("refuse un profil invalide et limite le nombre de profils", () => {
    const storage = memoryStorage();
    expect(() => saveProfile({ ...lea, age: 2 }, storage)).toThrow(LocalDataError);

    for (let index = 0; index < MAX_PROFILES; index++) {
      saveProfile({ ...lea, name: `Enfant ${index}` }, storage);
    }
    expect(() => saveProfile(lea, storage)).toThrow(/Au plus 20 profils/);
  });

  it("signale clairement un stockage plein", () => {
    const storage = memoryStorage();
    storage.setItem = () => {
      throw new DOMException("plein", "QuotaExceededError");
    };

    expect(() => saveProfile(lea, storage)).toThrow(/espace de stockage de ce navigateur est plein/);
  });
});
