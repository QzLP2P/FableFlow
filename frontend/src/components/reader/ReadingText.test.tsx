import { describe, expect, it } from "vitest";
import { renderWithProviders } from "../../test/renderWithProviders";
import { protectedWords } from "../../text/syllabify";
import { InlineReadingText, ReadingText } from "./ReadingText";

const text = "Léa ouvre la cabane.\n\nLe papillon s'envole.";

describe("ReadingText", () => {
  it("renders plain paragraphs without aids", () => {
    const { container } = renderWithProviders(<ReadingText text={text} readingAids={false} size="adult" />);

    const paragraphs = container.querySelectorAll("p");
    expect(paragraphs).toHaveLength(2);
    expect(paragraphs[0].textContent).toBe("Léa ouvre la cabane.");
    expect(container.querySelector(".ff-syl")).toBeNull();
  });

  it("exposes the original text once to assistive technology and decorates a hidden copy", () => {
    const { container } = renderWithProviders(
      <ReadingText text={text} readingAids size="child" protectedNames={protectedWords("Léa", [])} />,
    );

    for (const [index, paragraph] of ["Léa ouvre la cabane.", "Le papillon s'envole."].entries()) {
      const p = container.querySelectorAll("p")[index];
      const decorated = p.querySelector('[aria-hidden="true"]');
      expect(decorated?.textContent).toBe(paragraph);
      const accessible = Array.from(p.children).filter((child) => child.getAttribute("aria-hidden") !== "true");
      expect(accessible.map((child) => child.textContent)).toEqual([paragraph]);
    }

    const syllables = Array.from(container.querySelectorAll("p")[1].querySelectorAll(".ff-syl")).map((node) => node.textContent);
    expect(syllables).toEqual(expect.arrayContaining(["pa", "pil", "lon"]));
  });

  it("keeps the hero's name whole and alternates syllable colors", () => {
    const { container } = renderWithProviders(
      <ReadingText text="Léa cabane" readingAids size="child" protectedNames={protectedWords("Léa", [])} />,
    );

    const syllables = Array.from(container.querySelectorAll(".ff-syl"));
    expect(syllables.map((node) => node.textContent)).toEqual(["ca", "ba", "ne"]);
    expect(syllables.map((node) => node.getAttribute("data-syllable"))).toEqual(["a", "b", "a"]);
  });

  it("decorates inline labels the same way", () => {
    const { container } = renderWithProviders(<InlineReadingText text="le papillon" readingAids />);

    expect(Array.from(container.querySelectorAll(".ff-syl")).map((node) => node.textContent)).toEqual(["le", "pa", "pil", "lon"]);
    expect(container.querySelector('[aria-hidden="true"]')?.textContent).toBe("le papillon");
  });
});
