import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import type { Theme } from "@mui/material/styles";
import { Fragment, useMemo, type ReactNode } from "react";
import { segmentParagraph, splitParagraphs, type ReadingSegment } from "../../text/syllabify";

export type ReadingSize = "adult" | "child";

interface ReadingTextProps {
  text: string;
  readingAids: boolean;
  size: ReadingSize;
  /** Mots (en minuscules) jamais découpés, typiquement les prénoms. */
  protectedNames?: ReadonlySet<string>;
}

const visuallyHidden = {
  position: "absolute",
  width: 1,
  height: 1,
  overflow: "hidden",
  clip: "rect(0 0 0 0)",
  whiteSpace: "nowrap",
  userSelect: "none",
} as const;

const syllableSx = (theme: Theme) => ({
  "& .ff-word": { whiteSpace: "nowrap" },
  "& .ff-syl": {
    borderBottom: "2px solid currentColor",
    borderRadius: "0 0 0.55em 0.55em",
    paddingBottom: "0.04em",
    marginInline: "0.015em",
  },
  "& .ff-syl-a": { color: theme.palette.syllable.a },
  "& .ff-syl-b": { color: theme.palette.syllable.b },
});

const textSx = (size: ReadingSize, readingAids: boolean) => (theme: Theme) => ({
  "& p": {
    m: 0,
    mb: "1em",
    fontSize: size === "child" ? "clamp(1.35rem, 1.2rem + 0.8vw, 1.65rem)" : "clamp(1.125rem, 1.05rem + 0.4vw, 1.3rem)",
    lineHeight: size === "child" ? 1.95 : 1.75,
    letterSpacing: size === "child" ? "0.01em" : undefined,
    wordSpacing: readingAids ? "0.18em" : size === "child" ? "0.08em" : undefined,
  },
  "& p:last-of-type": { mb: 0 },
  ...syllableSx(theme),
});

function renderSegments(segments: ReadingSegment[]): ReactNode[] {
  let syllableIndex = 0;
  return segments.map((segment, index) => {
    if (segment.kind === "text" || !segment.syllables) {
      return <Fragment key={index}>{segment.text}</Fragment>;
    }

    return (
      <span key={index} className="ff-word">
        {segment.syllables.map((syllable, position) => {
          const variant = syllableIndex++ % 2 === 0 ? "a" : "b";
          return (
            <span key={position} className={`ff-syl ff-syl-${variant}`} data-syllable={variant}>
              {syllable}
            </span>
          );
        })}
      </span>
    );
  });
}

/**
 * Texte de lecture. Avec les aides, chaque syllabe écrite alterne deux couleurs et reçoit un arc ;
 * le texte brut reste exposé une seule fois aux lecteurs d'écran (la version décorée est masquée).
 * Les aides sont purement visuelles : le texte n'est jamais modifié.
 */
export function ReadingText({ text, readingAids, size, protectedNames }: ReadingTextProps) {
  const paragraphs = useMemo(() => splitParagraphs(text), [text]);
  const decorated = useMemo(
    () => (readingAids ? paragraphs.map((paragraph) => renderSegments(segmentParagraph(paragraph, protectedNames))) : null),
    [paragraphs, protectedNames, readingAids],
  );

  return (
    <Box sx={textSx(size, readingAids)}>
      {paragraphs.map((paragraph, index) => (
        <Typography key={index} component="p" sx={{ position: "relative" }}>
          {decorated ? (
            <>
              <Box component="span" sx={visuallyHidden}>
                {paragraph}
              </Box>
              <span aria-hidden="true">{decorated[index]}</span>
            </>
          ) : (
            paragraph
          )}
        </Typography>
      ))}
    </Box>
  );
}

/** Variante en ligne (libellés de choix, exemples) : même rendu, sans paragraphes. */
export function InlineReadingText({ text, readingAids, protectedNames }: Omit<ReadingTextProps, "size">) {
  const decorated = useMemo(
    () => (readingAids ? renderSegments(segmentParagraph(text, protectedNames)) : null),
    [text, protectedNames, readingAids],
  );

  if (!decorated) {
    return <>{text}</>;
  }

  return (
    <Box component="span" sx={(theme) => ({ position: "relative", ...syllableSx(theme) })}>
      <Box component="span" sx={visuallyHidden}>
        {text}
      </Box>
      <span aria-hidden="true">{decorated}</span>
    </Box>
  );
}
