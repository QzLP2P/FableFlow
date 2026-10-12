import { IDBFactory } from "fake-indexeddb";
import { describe, expect, it } from "vitest";
import type { AdventureHistoryDto } from "../api/types";
import { bookFromHistory, createIndexedDbBookStore, parseSavedBook, type SavedBook } from "./books";
import { LocalDataError } from "./localData";

function history(overrides: Partial<AdventureHistoryDto> = {}): AdventureHistoryDto {
  return {
    adventureId: "11111111-1111-1111-1111-111111111111",
    status: "Completed",
    title: "Le dragon de Léa",
    themeId: "dinosaur",
    premise: "Léa découvre un œuf de dinosaure.",
    length: "Short",
    readerMode: "ChildReader",
    hero: { name: "Léa", age: 6, companions: ["Moustache"], interests: null, personality: null, appearance: null },
    totalChapters: 3,
    chapters: [
      { number: 1, kind: "Narrative", text: "Il était une fois.", selectedChoiceLabel: null, illustration: { status: "Ready", url: "https://img/1.png" } },
      { number: 2, kind: "Decision", text: "Que faire ?", selectedChoiceLabel: "Suivre la piste", illustration: { status: "Failed", url: null } },
      { number: 3, kind: "Ending", text: "Et tout finit bien.", selectedChoiceLabel: null, illustration: { status: "NotRequested", url: null } },
    ],
    conclusion: "Et tout finit bien.",
    ...overrides,
  };
}

const freshStore = () => createIndexedDbBookStore(() => new IDBFactory());

function book(overrides: Partial<SavedBook> = {}): SavedBook {
  const snapshot = bookFromHistory(history(), { themeName: "Dinosaures", readingAids: true, now: new Date("2025-01-01T00:00:00Z") });
  if (!snapshot) {
    throw new Error("snapshot attendu");
  }
  return { ...snapshot, ...overrides };
}

describe("bookFromHistory", () => {
  it("fige le texte, le chemin choisi, la conclusion et les illustrations prêtes", () => {
    const snapshot = book();
    expect(snapshot).toMatchObject({
      id: history().adventureId,
      title: "Le dragon de Léa",
      themeName: "Dinosaures",
      readingAids: true,
      conclusion: "Et tout finit bien.",
    });
    expect(snapshot.chapters.map((c) => c.selectedChoiceLabel)).toEqual([null, "Suivre la piste", null]);
    expect(snapshot.chapters.map((c) => c.illustrationUrl)).toEqual(["https://img/1.png", null, null]);
  });

  it("n'enregistre rien tant que l'histoire n'est pas terminée", () => {
    expect(bookFromHistory(history({ status: "InProgress" }), { themeName: null, readingAids: false })).toBeNull();
  });

  it("donne un titre par défaut lorsque le titre manque", () => {
    expect(bookFromHistory(history({ title: null }), { themeName: null, readingAids: false })?.title).toBe("L'histoire de Léa");
  });
});

describe("parseSavedBook", () => {
  it("rejette les formats inconnus ou incohérents", () => {
    expect(parseSavedBook(book())).not.toBeNull();
    expect(parseSavedBook({ ...book(), schemaVersion: 2 })).toBeNull();
    expect(parseSavedBook({ ...book(), chapters: [] })).toBeNull();
    expect(parseSavedBook({ ...book(), chapters: book().chapters.slice(0, 2) })).toBeNull();
    expect(parseSavedBook({ ...book(), hero: { name: "" } })).toBeNull();
    expect(parseSavedBook("texte")).toBeNull();
  });
});

describe("IndexedDB book store", () => {
  it("enregistre puis relit un livre sans réseau", async () => {
    const store = freshStore();
    await store.put(book());

    const listing = await store.list();
    expect(listing.invalidIds).toEqual([]);
    expect(listing.books).toHaveLength(1);
    expect(await store.get(book().id)).toEqual(book());
  });

  it("remplace un livre existant au lieu de le dupliquer", async () => {
    const store = freshStore();
    await store.put(book());
    await store.put(book({ readingAids: false, savedAt: "2025-02-01T00:00:00.000Z" }));

    const { books } = await store.list();
    expect(books).toHaveLength(1);
    expect(books[0].readingAids).toBe(false);
  });

  it("trie par date d'enregistrement et supprime plusieurs livres", async () => {
    const store = freshStore();
    await store.put(book({ id: "a", savedAt: "2025-01-01T00:00:00.000Z" }));
    await store.put(book({ id: "b", savedAt: "2025-03-01T00:00:00.000Z" }));
    await store.put(book({ id: "c", savedAt: "2025-02-01T00:00:00.000Z" }));

    expect((await store.list()).books.map((b) => b.id)).toEqual(["b", "c", "a"]);
    await store.remove(["a", "b"]);
    expect((await store.list()).books.map((b) => b.id)).toEqual(["c"]);
  });

  it("isole les enregistrements illisibles", async () => {
    const factory = new IDBFactory();
    const store = createIndexedDbBookStore(() => factory);
    await store.put(book());

    await new Promise<void>((resolve, reject) => {
      const request = factory.open("fableflow", 1);
      request.onsuccess = () => {
        const transaction = request.result.transaction("books", "readwrite");
        transaction.objectStore("books").put({ id: "corrompu", schemaVersion: 1 });
        transaction.oncomplete = () => {
          request.result.close();
          resolve();
        };
        transaction.onerror = () => reject(transaction.error);
      };
      request.onerror = () => reject(request.error);
    });

    const listing = await store.list();
    expect(listing.books).toHaveLength(1);
    expect(listing.invalidIds).toEqual(["corrompu"]);
    expect(await store.get("corrompu")).toBeNull();
  });

  it("refuse un livre incomplet et signale l'absence d'IndexedDB", async () => {
    await expect(freshStore().put({ ...book(), chapters: [] })).rejects.toBeInstanceOf(LocalDataError);
    await expect(createIndexedDbBookStore(() => undefined).list()).rejects.toThrow(/ne permet pas d'enregistrer/);
  });
});
