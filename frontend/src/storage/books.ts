import type {
  AdventureHistoryDto,
  ChapterKind,
  HeroDto,
  ReaderMode,
  StoryLength,
} from "../api/types";
import { HERO_LIMITS, MAX_PREMISE_LENGTH, MAX_PREMISE_TITLE_LENGTH } from "../domain/hero";
import {
  LocalDataError,
  describeStorageFailure,
  isBoundedString,
  isOptionalString,
  isRecord,
} from "./localData";

/**
 * Bibliothèque des histoires terminées, conservée uniquement sur cet appareil (IndexedDB).
 * Chaque livre est un instantané autonome (texte intégral, chemin suivi, conclusion) : la relecture
 * ne dépend ni du serveur ni d'une nouvelle génération. Les URL d'illustration peuvent expirer.
 */
export const BOOK_SCHEMA_VERSION = 1;
const DATABASE_NAME = "fableflow";
const DATABASE_VERSION = 1;
const BOOK_STORE = "books";
const MAX_CHAPTERS = 20;
const MAX_CHAPTER_TEXT_LENGTH = 20_000;
const MAX_CHOICE_LABEL_LENGTH = 200;
const MAX_IMAGE_URL_LENGTH = 15_000_000;

export interface SavedBookChapter {
  number: number;
  kind: ChapterKind;
  text: string;
  /** Action retenue à ce moment de choix, sinon `null`. */
  selectedChoiceLabel: string | null;
  /** Illustration prête au moment de l'enregistrement (peut expirer), sinon `null`. */
  illustrationUrl: string | null;
}

export interface SavedBook {
  schemaVersion: typeof BOOK_SCHEMA_VERSION;
  id: string;
  title: string;
  themeId: string;
  themeName: string | null;
  premise: string;
  length: StoryLength;
  readerMode: ReaderMode;
  readingAids: boolean;
  hero: HeroDto;
  chapters: SavedBookChapter[];
  conclusion: string;
  savedAt: string;
}

export interface BookListing {
  books: SavedBook[];
  /** Identifiants des enregistrements illisibles (format inconnu ou corrompu). */
  invalidIds: string[];
}

export interface BookStore {
  list(): Promise<BookListing>;
  get(id: string): Promise<SavedBook | null>;
  /** Crée ou remplace le livre portant le même identifiant (jamais de doublon). */
  put(book: SavedBook): Promise<void>;
  remove(ids: readonly string[]): Promise<void>;
}

const chapterKinds: readonly ChapterKind[] = ["Narrative", "Decision", "Ending"];

function parseHero(value: unknown): HeroDto | null {
  if (!isRecord(value)) {
    return null;
  }

  const { name, age, companions, interests, personality, appearance } = value;
  const valid =
    isBoundedString(name, HERO_LIMITS.maxNameLength) &&
    typeof age === "number" &&
    Number.isInteger(age) &&
    Array.isArray(companions) &&
    companions.length <= HERO_LIMITS.maxCompanions &&
    companions.every((c) => isBoundedString(c, HERO_LIMITS.maxCompanionLength)) &&
    isOptionalString(interests, HERO_LIMITS.maxDetailLength) &&
    isOptionalString(personality, HERO_LIMITS.maxDetailLength) &&
    isOptionalString(appearance, HERO_LIMITS.maxDetailLength);

  return valid
    ? { name, age, companions: [...(companions as string[])], interests, personality, appearance }
    : null;
}

function parseChapter(value: unknown, expectedNumber: number): SavedBookChapter | null {
  if (!isRecord(value)) {
    return null;
  }

  const { number, kind, text, selectedChoiceLabel, illustrationUrl } = value;
  const valid =
    number === expectedNumber &&
    typeof kind === "string" &&
    chapterKinds.includes(kind as ChapterKind) &&
    isBoundedString(text, MAX_CHAPTER_TEXT_LENGTH) &&
    isOptionalString(selectedChoiceLabel, MAX_CHOICE_LABEL_LENGTH) &&
    isOptionalString(illustrationUrl, MAX_IMAGE_URL_LENGTH);

  return valid
    ? {
        number,
        kind: kind as ChapterKind,
        text,
        selectedChoiceLabel,
        illustrationUrl: illustrationUrl || null,
      }
    : null;
}

/** Revalide un livre lu depuis le stockage ; `null` s'il est illisible ou d'un format inconnu. */
export function parseSavedBook(value: unknown): SavedBook | null {
  if (!isRecord(value) || value.schemaVersion !== BOOK_SCHEMA_VERSION) {
    return null;
  }

  const { id, title, themeId, themeName, premise, length, readerMode, readingAids, conclusion, savedAt } = value;
  const hero = parseHero(value.hero);
  const rawChapters = value.chapters;
  if (
    !hero ||
    !isBoundedString(id, 100) ||
    !isBoundedString(title, MAX_PREMISE_TITLE_LENGTH) ||
    !isBoundedString(themeId, 100) ||
    !isOptionalString(themeName, 200) ||
    !isBoundedString(premise, MAX_PREMISE_LENGTH) ||
    (length !== "Short" && length !== "Longer") ||
    (readerMode !== "AdultReadAloud" && readerMode !== "ChildReader") ||
    typeof readingAids !== "boolean" ||
    !isBoundedString(conclusion, MAX_CHAPTER_TEXT_LENGTH) ||
    !isBoundedString(savedAt, 40) ||
    !Array.isArray(rawChapters) ||
    rawChapters.length === 0 ||
    rawChapters.length > MAX_CHAPTERS
  ) {
    return null;
  }

  const chapters: SavedBookChapter[] = [];
  for (const [index, raw] of rawChapters.entries()) {
    const chapter = parseChapter(raw, index + 1);
    if (!chapter) {
      return null;
    }
    chapters.push(chapter);
  }

  if (chapters[chapters.length - 1].kind !== "Ending") {
    return null;
  }

  return {
    schemaVersion: BOOK_SCHEMA_VERSION,
    id,
    title,
    themeId,
    themeName,
    premise,
    length,
    readerMode,
    readingAids,
    hero,
    chapters,
    conclusion,
    savedAt,
  };
}

/**
 * Construit l'instantané d'une histoire terminée à partir de son historique serveur ;
 * `null` tant que l'histoire n'est pas terminée.
 */
export function bookFromHistory(
  history: AdventureHistoryDto,
  options: { themeName: string | null; readingAids: boolean; now?: Date },
): SavedBook | null {
  const ending = history.chapters[history.chapters.length - 1];
  if (history.status !== "Completed" || !ending || ending.kind !== "Ending") {
    return null;
  }

  return parseSavedBook({
    schemaVersion: BOOK_SCHEMA_VERSION,
    id: history.adventureId,
    title: history.title?.trim() || `L'histoire de ${history.hero.name}`,
    themeId: history.themeId,
    themeName: options.themeName,
    premise: history.premise,
    length: history.length,
    readerMode: history.readerMode,
    readingAids: options.readingAids,
    hero: history.hero,
    chapters: history.chapters.map((chapter) => ({
      number: chapter.number,
      kind: chapter.kind,
      text: chapter.text,
      selectedChoiceLabel: chapter.selectedChoiceLabel,
      illustrationUrl: chapter.illustration.status === "Ready" ? chapter.illustration.url : null,
    })),
    conclusion: history.conclusion ?? ending.text,
    savedAt: (options.now ?? new Date()).toISOString(),
  });
}

function requestToPromise<T>(request: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

function transactionDone(transaction: IDBTransaction): Promise<void> {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error);
    transaction.onabort = () => reject(transaction.error ?? new DOMException("Transaction annulée", "AbortError"));
  });
}

/** Magasin IndexedDB ; la base est ouverte à la demande et réutilisée. */
export function createIndexedDbBookStore(
  factory: () => IDBFactory | undefined = () => (typeof indexedDB === "undefined" ? undefined : indexedDB),
): BookStore {
  let databasePromise: Promise<IDBDatabase> | null = null;

  function open(): Promise<IDBDatabase> {
    if (!databasePromise) {
      databasePromise = new Promise<IDBDatabase>((resolve, reject) => {
        const idb = factory();
        if (!idb) {
          reject(new LocalDataError("Ce navigateur ne permet pas d'enregistrer des histoires sur l'appareil."));
          return;
        }

        const request = idb.open(DATABASE_NAME, DATABASE_VERSION);
        request.onupgradeneeded = () => {
          if (!request.result.objectStoreNames.contains(BOOK_STORE)) {
            request.result.createObjectStore(BOOK_STORE, { keyPath: "id" });
          }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new LocalDataError("La bibliothèque est utilisée par un autre onglet. Fermez-le puis réessayez."));
      }).catch((error: unknown) => {
        databasePromise = null;
        throw new LocalDataError(describeStorageFailure(error), { cause: error });
      });
    }

    return databasePromise;
  }

  async function run<T>(
    mode: IDBTransactionMode,
    action: (store: IDBObjectStore) => IDBRequest<T> | null,
    failureMessage: string,
  ): Promise<T | undefined> {
    const database = await open();
    try {
      const transaction = database.transaction(BOOK_STORE, mode);
      const request = action(transaction.objectStore(BOOK_STORE));
      const [result] = await Promise.all([request ? requestToPromise(request) : Promise.resolve(undefined), transactionDone(transaction)]);
      return result;
    } catch (error) {
      throw new LocalDataError(`${failureMessage} ${describeStorageFailure(error)}`, { cause: error });
    }
  }

  return {
    async list() {
      const database = await open();
      try {
        const transaction = database.transaction(BOOK_STORE, "readonly");
        const store = transaction.objectStore(BOOK_STORE);
        const [keys, values] = await Promise.all([
          requestToPromise(store.getAllKeys()),
          requestToPromise(store.getAll()),
        ]);

        const books: SavedBook[] = [];
        const invalidIds: string[] = [];
        values.forEach((value, index) => {
          const book = parseSavedBook(value);
          if (book) {
            books.push(book);
          } else {
            invalidIds.push(String(keys[index]));
          }
        });

        books.sort((a, b) => b.savedAt.localeCompare(a.savedAt));
        return { books, invalidIds };
      } catch (error) {
        throw new LocalDataError(`La bibliothèque n'a pas pu être lue. ${describeStorageFailure(error)}`, { cause: error });
      }
    },

    async get(id) {
      const value = await run("readonly", (store) => store.get(id), "Cette histoire n'a pas pu être lue.");
      return value === undefined ? null : parseSavedBook(value);
    },

    async put(book) {
      const valid = parseSavedBook(book);
      if (!valid) {
        throw new LocalDataError("Cette histoire est incomplète et n'a pas été enregistrée.");
      }

      await run("readwrite", (store) => store.put(valid), "L'histoire n'a pas pu être enregistrée sur cet appareil.");
    },

    async remove(ids) {
      await run(
        "readwrite",
        (store) => {
          ids.forEach((id) => store.delete(id));
          return null;
        },
        "La suppression n'a pas abouti.",
      );
    },
  };
}

export const bookStore: BookStore = createIndexedDbBookStore();
