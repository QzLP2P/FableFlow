import { keyframes } from "@emotion/react";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import FormControlLabel from "@mui/material/FormControlLabel";
import LinearProgress from "@mui/material/LinearProgress";
import Switch from "@mui/material/Switch";
import Typography from "@mui/material/Typography";
import type { ReactNode, Ref } from "react";
import type { ChapterKind, IllustrationDto, ReaderMode } from "../../api/types";
import { ChapterIllustration } from "./ChapterIllustration";
import { ReadingText } from "./ReadingText";

/** Seul mouvement de l'application : l'arrivée douce d'un chapitre (désactivée si mouvement réduit). */
const chapterIn = keyframes`
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: none; }
`;

export interface ReaderChapterView {
  number: number;
  kind: ChapterKind;
  text: string;
  selectedChoiceLabel: string | null;
  illustration: IllustrationDto | null;
}

interface BookReaderProps {
  title: string;
  subtitle: string;
  chapter: ReaderChapterView;
  totalChapters: number;
  /** Nombre de chapitres déjà écrits (navigables). */
  readableChapters: number;
  onNavigate: (chapterNumber: number) => void;
  readerMode: ReaderMode;
  readingAids: boolean;
  onReadingAidsChange: (value: boolean) => void;
  protectedNames: ReadonlySet<string>;
  showIllustrations: boolean;
  /** Vrai pour le dernier chapitre écrit d'une histoire en cours : le choix n'est pas encore fait. */
  isFrontier: boolean;
  headingRef?: Ref<HTMLHeadingElement>;
  actions?: ReactNode;
}

/**
 * Lecteur de chapitre partagé entre l'histoire en cours et un livre enregistré : texte lisible
 * (65–75 caractères par ligne), illustration à côté sur grand écran et au-dessus sur mobile.
 */
export function BookReader({
  title,
  subtitle,
  chapter,
  totalChapters,
  readableChapters,
  onNavigate,
  readerMode,
  readingAids,
  onReadingAidsChange,
  protectedNames,
  showIllustrations,
  isFrontier,
  headingRef,
  actions,
}: BookReaderProps) {
  const progress = Math.round((chapter.number / totalChapters) * 100);
  const isLastChapter = chapter.kind === "Ending";
  const illustration = showIllustrations ? chapter.illustration : null;

  return (
    <Container maxWidth={illustration ? "lg" : "md"} sx={{ pt: { xs: 2, md: 4 }, pb: "calc(env(safe-area-inset-bottom) + 3rem)" }}>
      <Box component="header" sx={{ mb: 2, maxWidth: "70ch" }}>
        <Typography variant="h1" sx={{ fontSize: "clamp(1.6rem, 1.3rem + 1.4vw, 2.4rem)", lineHeight: 1.2 }}>
          {title}
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          {subtitle}
        </Typography>
      </Box>

      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", columnGap: 2 }}>
        <Typography variant="body2" color="text.secondary">
          Chapitre {chapter.number} sur {totalChapters}
        </Typography>
        <FormControlLabel
          sx={{ mr: 0, minHeight: 48 }}
          control={<Switch checked={readingAids} onChange={(event) => onReadingAidsChange(event.target.checked)} />}
          label="Aides à la lecture"
        />
      </Box>
      <LinearProgress
        variant="determinate"
        value={progress}
        aria-label={`Progression : chapitre ${chapter.number} sur ${totalChapters}`}
        sx={{ mb: { xs: 3, md: 4 }, height: 6, borderRadius: "16px" }}
      />

      <Box
        sx={{
          display: "grid",
          gap: { xs: 3, md: 6 },
          alignItems: "start",
          gridTemplateColumns: illustration ? { xs: "minmax(0, 1fr)", md: "minmax(0, 5fr) minmax(0, 7fr)" } : "minmax(0, 1fr)",
        }}
      >
        {illustration && (
          <Box sx={{ position: { md: "sticky" }, top: { md: 88 } }}>
            <ChapterIllustration
              status={illustration.status}
              url={illustration.url}
              alt={`Illustration du chapitre ${chapter.number}`}
            />
          </Box>
        )}

        <Box
          key={chapter.number}
          component="article"
          aria-labelledby="chapter-heading"
          sx={{
            width: "100%",
            maxWidth: "70ch",
            animation: `${chapterIn} 320ms cubic-bezier(0.2, 0.8, 0.2, 1)`,
            "@media (prefers-reduced-motion: reduce)": { animation: "none" },
          }}
        >
          <Typography
            id="chapter-heading"
            ref={headingRef}
            tabIndex={-1}
            variant="h2"
            sx={{
              fontSize: "clamp(1.25rem, 1.1rem + 0.6vw, 1.6rem)",
              mb: 2,
              "&:focus": { outline: "none" },
              "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main", outlineOffset: 4 },
            }}
          >
            {isLastChapter ? `Chapitre ${chapter.number} — Le dénouement` : `Chapitre ${chapter.number}`}
          </Typography>

          <ReadingText
            text={chapter.text}
            readingAids={readingAids}
            size={readerMode === "ChildReader" ? "child" : "adult"}
            protectedNames={protectedNames}
          />

          {!isFrontier && chapter.selectedChoiceLabel && (
            <Box sx={{ mt: 3, px: 2, py: 1.5, borderRadius: "12px", bgcolor: "action.hover" }}>
              <Typography>
                <Box component="span" sx={{ fontWeight: 700 }}>
                  Choix fait :
                </Box>{" "}
                {chapter.selectedChoiceLabel}
              </Typography>
            </Box>
          )}

          {actions && <Box sx={{ mt: 4 }}>{actions}</Box>}

          <Box
            component="nav"
            aria-label="Chapitres"
            sx={{ mt: 4, display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 1 }}
          >
            <Button
              startIcon={<ArrowBackRoundedIcon />}
              disabled={chapter.number <= 1}
              onClick={() => onNavigate(chapter.number - 1)}
            >
              Chapitre précédent
            </Button>
            {chapter.number < readableChapters && (
              <Button endIcon={<ArrowForwardRoundedIcon />} onClick={() => onNavigate(chapter.number + 1)}>
                Chapitre suivant
              </Button>
            )}
          </Box>
        </Box>
      </Box>
    </Container>
  );
}
