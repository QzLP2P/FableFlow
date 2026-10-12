import { describe, expect, it } from "vitest";
import { protectedWords, segmentParagraph, splitParagraphs, syllabifyWord } from "./syllabify";

const split = (word: string, keep?: ReadonlySet<string>) => syllabifyWord(word, keep)?.join("|") ?? null;

describe("syllabifyWord", () => {
  it.each([
    ["papa", "pa|pa"],
    ["papillon", "pa|pil|lon"],
    ["chocolat", "cho|co|lat"],
    ["montagne", "mon|ta|gne"],
    ["éléphant", "é|lé|phant"],
    ["oiseau", "oi|seau"],
    ["chat", "chat"],
    ["enfants", "en|fants"],
    ["chanter", "chan|ter"],
    ["année", "an|née"],
  ])("découpe les mots courants : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each([
    ["lune", "lu|ne"],
    ["arbre", "ar|bre"],
    ["pommes", "pom|mes"],
    ["chantent", "chan|tent"],
    ["île", "î|le"],
  ])("garde le e muet final comme syllabe écrite : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each([
    ["table", "ta|ble"],
    ["comprendre", "com|pren|dre"],
    ["instant", "ins|tant"],
    ["obscur", "obs|cur"],
    ["construire", "cons|trui|re"],
    ["ouvrir", "ou|vrir"],
    ["esprit", "es|prit"],
  ])("respecte les groupes de consonnes : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each([
    ["avion", "a|vion"],
    ["chien", "chien"],
    ["fée", "fée"],
    ["fées", "fées"],
    ["naïf", "na|ïf"],
    ["Noël", "No|ël"],
    ["réalité", "ré|a|li|té"],
    ["théâtre", "thé|â|tre"],
    ["poème", "po|è|me"],
    ["aéroport", "a|é|ro|port"],
    ["poésie", "po|é|sie"],
  ])("gère accents, tréma et voyelles contiguës : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each([
    ["quatre", "qua|tre"],
    ["guerre", "guer|re"],
    ["langue", "lan|gue"],
    ["pigeon", "pi|geon"],
  ])("traite qu, gu et ge comme une consonne : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each([
    ["l'arbre", "l'ar|bre"],
    ["L'arbre", "L'ar|bre"],
    ["aujourd'hui", "au|jour|d'hui"],
    ["jusqu'à", "jus|qu'à"],
    ["presqu'île", "pres|qu'î|le"],
    ["lorsqu'elle", "lors|qu'el|le"],
    ["qu'il", "qu'il"],
    ["c’est", "c’est"],
    ["d'éléphant", "d'é|lé|phant"],
  ])("colle l'élision à la syllabe suivante : %s", (word, expected) => {
    expect(split(word)).toBe(expected);
  });

  it.each(["feuille", "paille", "crayon", "voyage", "ADN", "McQueen", "piñata", "Zoë2", "cafe\u0301", "brr"])(
    "laisse intact un mot incertain : %s",
    (word) => {
      expect(syllabifyWord(word)).toBeNull();
    },
  );

  it("laisse intacts les prénoms protégés, y compris après une élision", () => {
    const keep = protectedWords("Amélie");
    expect(syllabifyWord("Amélie", keep)).toBeNull();
    expect(syllabifyWord("d'Amélie", keep)).toBeNull();
    expect(split("Pikachu", keep)).toBe("Pi|ka|chu");
  });

  it("reconstruit toujours le mot exact", () => {
    const words = ["Aujourd'hui", "Éléphant", "œuf", "cœur", "Pikachu", "dinosaures", "vétérinaire", "araignée"];
    for (const word of words) {
      const syllables = syllabifyWord(word);
      expect(syllables).not.toBeNull();
      expect(syllables?.join("")).toBe(word);
    }
  });
});

describe("protectedWords", () => {
  it("protège le prénom et les noms propres des compagnons sans protéger les mots courants", () => {
    const keep = protectedWords("Léa Martin", ["le chat Moustache", "Papi"]);
    expect([...keep].sort()).toEqual(["léa", "martin", "moustache", "papi"]);
  });
});

describe("segmentParagraph", () => {
  it("sépare mots, ponctuation et traits d'union sans perdre de caractère", () => {
    const paragraph = "« Bonjour, arc-en-ciel ! » dit Léa.";
    const segments = segmentParagraph(paragraph, protectedWords("Léa"));

    expect(segments.map((s) => s.text).join("")).toBe(paragraph);
    const words = segments.filter((s) => s.kind === "word");
    expect(words.map((w) => w.text)).toEqual(["Bonjour", "arc", "en", "ciel", "dit", "Léa"]);
    expect(words.find((w) => w.text === "Bonjour")?.syllables).toEqual(["Bon", "jour"]);
    expect(words.find((w) => w.text === "Léa")?.syllables).toBeNull();
  });

  it("isole les chiffres sans les découper", () => {
    const segments = segmentParagraph("Il a 7 ans.");
    expect(segments.map((s) => s.text).join("")).toBe("Il a 7 ans.");
    expect(segments.some((s) => s.kind === "word" && s.text.includes("7"))).toBe(false);
  });
});

describe("splitParagraphs", () => {
  it("ignore les lignes vides et normalise les espaces", () => {
    expect(splitParagraphs("Il était une fois.\n\n  Un  dragon.\r\nFin.")).toEqual([
      "Il était une fois.",
      "Un dragon.",
      "Fin.",
    ]);
  });

  it("applique les espaces insécables de la ponctuation française", () => {
    expect(splitParagraphs("«Tu entends ça ?» murmure Léa : « Viens ! »")).toEqual([
      "«\u00A0Tu entends ça\u00A0?\u00A0» murmure Léa\u00A0: «\u00A0Viens\u00A0!\u00A0»",
    ]);
  });
});
