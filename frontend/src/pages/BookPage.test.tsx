import { fireEvent, screen } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { bookStore, type SavedBook } from "../storage/books";
import { renderWithProviders } from "../test/renderWithProviders";
import { BookPage, SavedBookReader } from "./BookPage";

const book: SavedBook = {
  schemaVersion: 1,
  id: "book-1",
  title: "L'œuf qui brille",
  themeId: "pokemon",
  themeName: "Pokémon — Apprenti dresseur",
  premise: "Léa trouve un œuf lumineux.",
  length: "Short",
  readerMode: "AdultReadAloud",
  readingAids: false,
  hero: { name: "Léa", age: 6, companions: ["Biscuit"], interests: null, personality: null, appearance: null },
  chapters: [
    { number: 1, kind: "Narrative", text: "Léa trouve un œuf dans le jardin.", selectedChoiceLabel: null, illustrationUrl: null },
    { number: 2, kind: "Decision", text: "L'œuf se met à trembler.", selectedChoiceLabel: "Appeler Biscuit", illustrationUrl: null },
    { number: 3, kind: "Ending", text: "Une petite créature sourit à Léa.", selectedChoiceLabel: null, illustrationUrl: null },
  ],
  conclusion: "Une amitié est née.",
  savedAt: "2026-03-12T19:00:00.000Z",
};

describe("SavedBookReader", () => {
  it("replays the saved path as a linear book without active choices", () => {
    renderWithProviders(<SavedBookReader book={book} />);

    expect(screen.getByRole("heading", { level: 1, name: book.title })).toBeDefined();
    expect(screen.getByText("Léa trouve un œuf dans le jardin.")).toBeDefined();
    expect((screen.getByRole("button", { name: "Chapitre précédent" }) as HTMLButtonElement).disabled).toBe(true);

    fireEvent.click(screen.getByRole("button", { name: "Chapitre suivant" }));
    expect(screen.getByText("L'œuf se met à trembler.")).toBeDefined();
    expect(screen.getByText("Appeler Biscuit")).toBeDefined();
    expect(screen.queryByRole("button", { name: /Appeler Biscuit/ })).toBeNull();
    expect(screen.queryByText(/Que va faire/)).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Chapitre suivant" }));
    expect(screen.getByRole("heading", { name: "Fin" })).toBeDefined();
    expect(screen.queryByRole("button", { name: "Chapitre suivant" })).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Relire depuis le début" }));
    expect(screen.getByText("Léa trouve un œuf dans le jardin.")).toBeDefined();
  });

  it("does not show an illustration column when no image was saved", () => {
    renderWithProviders(<SavedBookReader book={book} />);
    expect(screen.queryByText("Illustration indisponible")).toBeNull();
  });
});

describe("BookPage", () => {
  beforeEach(async () => {
    const { books } = await bookStore.list();
    await bookStore.remove(books.map((b) => b.id));
  });

  it("opens a book saved on this device", async () => {
    await bookStore.put(book);
    renderWithProviders(
      <Routes>
        <Route path="/library/:bookId" element={<BookPage />} />
      </Routes>,
      { route: "/library/book-1" },
    );

    expect(await screen.findByRole("heading", { level: 1, name: book.title })).toBeDefined();
  });

  it("explains when the book is not on this device", async () => {
    renderWithProviders(
      <Routes>
        <Route path="/library/:bookId" element={<BookPage />} />
      </Routes>,
      { route: "/library/missing" },
    );

    expect(await screen.findByRole("heading", { name: "Livre introuvable" })).toBeDefined();
    expect(screen.getByRole("link", { name: "Retour à la bibliothèque" })).toBeDefined();
  });
});
