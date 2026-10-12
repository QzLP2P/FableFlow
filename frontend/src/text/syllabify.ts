/**
 * Découpage syllabique écrit du français, pensé comme aide visuelle à la lecture
 * (alternance de couleurs, arc sous chaque syllabe) et non comme découpage phonétique.
 *
 * Conventions retenues (proches des manuels de lecture) :
 * - le « e » final muet forme sa propre syllabe écrite : lu-ne, ar-bre ;
 * - les voyelles contiguës restent ensemble (a-vion, chien, fée), sauf tréma (na-ïf),
 *   « é » suivi d'une voyelle prononcée (ré-a-li-té) et a/o suivis de é/è/ê (po-è-me) ;
 * - une consonne entre deux voyelles ouvre la syllabe suivante (pa-pa) ; deux consonnes se
 *   séparent (pom-me), sauf les groupes inséparables consonne + l/r (ta-ble, com-pren-dre) ;
 * - ch, ph, gn, th, rh, qu (+ voyelle), gu (+ e/i), ge (+ a/o/u) comptent pour une consonne ;
 * - l'élision reste collée à la syllabe suivante : l'ar-bre, au-jour-d'hui, jus-qu'à.
 *
 * Par prudence, certains mots sont laissés intacts (`null`) plutôt que mal découpés :
 * chiffres, lettres étrangères ou diacritiques combinants, majuscules internes (sigles),
 * prénoms protégés, « ill » après voyelle (feuille) et « y » entre voyelles (crayon).
 * Le découpage reconstruit toujours le mot exact : `syllables.join("") === word`.
 */

const VOWELS = new Set("aeiouyàâäéèêëîïôöùûüÿæœ");
const LETTERS = new Set("abcdefghijklmnopqrstuvwxyzàâäçéèêëîïôöùûüÿæœ");
const TREMA = new Set("ëïüÿ");
const CONSONANT_DIGRAPHS = new Set(["ch", "ph", "gn", "th", "rh"]);
const CLUSTER_FIRST = new Set(["b", "c", "d", "f", "g", "k", "p", "t", "v", "ph", "ch", "th"]);
const CLUSTER_SECOND = new Set(["l", "r"]);
const APOSTROPHES = /['’]/;
const VOWEL_CLASS = "aeiouàâäéèêëîïôöùûü";
const ILL_AFTER_VOWEL = new RegExp(`[${VOWEL_CLASS}]ill`);
const Y_BETWEEN_VOWELS = new RegExp(`[${VOWEL_CLASS}]y[${VOWEL_CLASS}y]`);

/** Un mot : lettres, éventuellement reliées par des apostrophes (élisions). */
const WORD_PATTERN = /[\p{L}\p{M}]+(?:['’][\p{L}\p{M}]+)*/gu;

interface Grapheme {
  start: number;
  text: string;
  vowel: boolean;
}

function isVowel(char: string | undefined): boolean {
  return char !== undefined && VOWELS.has(char);
}

function toGraphemes(lower: string): Grapheme[] {
  const graphemes: Grapheme[] = [];
  let index = 0;
  while (index < lower.length) {
    const char = lower[index];
    const pair = lower.slice(index, index + 2);
    const next = lower[index + 2];
    const isConsonantPair =
      CONSONANT_DIGRAPHS.has(pair) ||
      (pair === "qu" && isVowel(next)) ||
      (pair === "gu" && next !== undefined && "eéèêiîy".includes(next)) ||
      (pair === "ge" && next !== undefined && "aouâô".includes(next));

    if (isConsonantPair) {
      graphemes.push({ start: index, text: pair, vowel: false });
      index += 2;
    } else {
      graphemes.push({ start: index, text: char, vowel: isVowel(char) });
      index += 1;
    }
  }
  return graphemes;
}

/** Vrai si deux voyelles contiguës appartiennent à des syllabes écrites différentes. */
function isHiatus(lower: string, previous: Grapheme, next: Grapheme): boolean {
  if (TREMA.has(next.text)) {
    return true;
  }

  if (previous.text === "é") {
    const rest = lower.slice(next.start + 1);
    const silentEnding = next.text === "e" && (rest === "" || rest === "s" || rest === "nt");
    return !silentEnding;
  }

  return (previous.text === "a" || previous.text === "o") && "éèê".includes(next.text);
}

/** Positions (indices de caractères) où commence chaque syllabe, ou `null` sans voyelle. */
function syllableStarts(lower: string): number[] | null {
  const graphemes = toGraphemes(lower);
  const nuclei: Array<{ first: number; last: number }> = [];

  graphemes.forEach((grapheme, index) => {
    if (!grapheme.vowel) {
      return;
    }

    const current = nuclei[nuclei.length - 1];
    if (current && current.last === index - 1 && !isHiatus(lower, graphemes[index - 1], grapheme)) {
      current.last = index;
    } else {
      nuclei.push({ first: index, last: index });
    }
  });

  if (nuclei.length === 0) {
    return null;
  }

  const starts = [0];
  for (let k = 1; k < nuclei.length; k++) {
    const consonants = graphemes.slice(nuclei[k - 1].last + 1, nuclei[k].first);
    let boundary: Grapheme;
    if (consonants.length === 0) {
      boundary = graphemes[nuclei[k].first];
    } else if (consonants.length === 1) {
      boundary = consonants[0];
    } else {
      const beforeLast = consonants[consonants.length - 2];
      const last = consonants[consonants.length - 1];
      boundary =
        CLUSTER_FIRST.has(beforeLast.text) && CLUSTER_SECOND.has(last.text) ? beforeLast : last;
    }
    starts.push(boundary.start);
  }
  return starts;
}

function sliceAt(text: string, starts: number[]): string[] {
  return starts.map((start, index) => text.slice(start, starts[index + 1]));
}

/** Découpe un fragment sans apostrophe déjà validé. */
function syllabifySimple(original: string): string[] | null {
  const lower = original.toLowerCase();
  if (ILL_AFTER_VOWEL.test(lower) || Y_BETWEEN_VOWELS.test(lower)) {
    return null;
  }

  const starts = syllableStarts(lower);
  return starts ? sliceAt(original, starts) : null;
}

function isSupported(word: string, lower: string): boolean {
  if (lower.length !== word.length) {
    return false;
  }

  for (let index = 0; index < word.length; index++) {
    const char = lower[index];
    if (APOSTROPHES.test(char)) {
      continue;
    }
    if (!LETTERS.has(char) || (index > 0 && word[index] !== char)) {
      return false;
    }
  }
  return true;
}

/**
 * Découpe un mot en syllabes écrites, ou renvoie `null` pour le laisser intact.
 * @param keepIntact mots (en minuscules) à ne jamais découper, par exemple les prénoms.
 */
export function syllabifyWord(word: string, keepIntact: ReadonlySet<string> = new Set()): string[] | null {
  const lower = word.toLowerCase();
  if (!word || !isSupported(word, lower)) {
    return null;
  }

  const parts = word.split(/(?<=['’])/u);
  if (parts.some((part) => keepIntact.has(part.replace(APOSTROPHES, "").toLowerCase()))) {
    return null;
  }

  const syllables: string[] = [];
  let glue = "";
  for (const [index, part] of parts.entries()) {
    const isLast = index === parts.length - 1;
    const letters = isLast ? part : part.slice(0, -1);
    const apostrophe = isLast ? "" : part.slice(-1);
    const letterLower = letters.toLowerCase();

    let elided = "";
    let core = letters;
    const endsWithQu = letterLower.endsWith("qu");
    if (!isLast && (endsWithQu || !isVowel(letterLower[letterLower.length - 1]))) {
      const graphemeLength = endsWithQu ? 2 : 1;
      elided = letters.slice(-graphemeLength);
      core = letters.slice(0, -graphemeLength);
      if (!core.split("").some((char) => isVowel(char.toLowerCase()))) {
        elided = letters;
        core = "";
      }
    }

    const coreSyllables = core ? syllabifySimple(core) : [];
    if (coreSyllables === null) {
      return null;
    }

    if (glue) {
      if (coreSyllables.length === 0) {
        glue += elided + apostrophe;
        continue;
      }
      coreSyllables[0] = glue + coreSyllables[0];
      glue = "";
    }

    if (isLast) {
      syllables.push(...coreSyllables);
    } else if (elided) {
      syllables.push(...coreSyllables);
      glue = elided + apostrophe;
    } else if (coreSyllables.length > 0) {
      coreSyllables[coreSyllables.length - 1] += apostrophe;
      syllables.push(...coreSyllables);
    } else {
      return null;
    }
  }

  if (glue || syllables.length === 0 || syllables.join("") !== word) {
    return null;
  }

  return syllables;
}

export type ReadingSegment =
  | { kind: "text"; text: string }
  | { kind: "word"; text: string; syllables: string[] | null };

/** Découpe un paragraphe en mots (syllabés ou intacts) et en ponctuation/espaces. */
export function segmentParagraph(paragraph: string, keepIntact: ReadonlySet<string> = new Set()): ReadingSegment[] {
  const segments: ReadingSegment[] = [];
  let cursor = 0;
  for (const match of paragraph.matchAll(WORD_PATTERN)) {
    const start = match.index;
    if (start > cursor) {
      segments.push({ kind: "text", text: paragraph.slice(cursor, start) });
    }
    segments.push({ kind: "word", text: match[0], syllables: syllabifyWord(match[0], keepIntact) });
    cursor = start + match[0].length;
  }

  if (cursor < paragraph.length) {
    segments.push({ kind: "text", text: paragraph.slice(cursor) });
  }
  return segments;
}

/** Paragraphes non vides d'un chapitre (une ligne = un paragraphe). */
export function splitParagraphs(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((line) => withFrenchSpacing(line.replace(/\s+/g, " ").trim()))
    .filter(Boolean);
}

/**
 * Espaces insécables de la typographie française : « et » ne se retrouvent jamais seuls
 * en bout de ligne, ni ? ! : ; en début de ligne.
 */
function withFrenchSpacing(line: string): string {
  return line.replace(/« ?/g, "«\u00A0").replace(/ ?»/g, "\u00A0»").replace(/ ([?!:;])/g, "\u00A0$1");
}

/**
 * Mots à ne jamais découper (en minuscules) : le prénom du héros, et pour chaque compagnon
 * soit son nom s'il tient en un mot, soit ses mots à majuscule (« le chat Moustache » → moustache).
 */
export function protectedWords(heroName: string, companions: readonly string[] = []): Set<string> {
  const words = new Set<string>();
  const add = (value: string, onlyCapitalized: boolean) => {
    const parts = [...value.matchAll(WORD_PATTERN)].flatMap((match) => match[0].split(APOSTROPHES));
    const keepAll = !onlyCapitalized || parts.length === 1;
    for (const part of parts) {
      if (part && (keepAll || part[0] !== part[0].toLowerCase())) {
        words.add(part.toLowerCase());
      }
    }
  };

  add(heroName, false);
  companions.forEach((companion) => add(companion, true));
  return words;
}
