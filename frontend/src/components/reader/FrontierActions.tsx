import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";
import type { ChapterDto } from "../../api/types";
import type { FrontierAction } from "../../domain/reader";
import { InlineReadingText } from "./ReadingText";

export function WritingStatus({ label }: { label: string }) {
  return (
    <Box role="status" sx={{ display: "flex", alignItems: "center", gap: 1.5, minHeight: 48 }}>
      <CircularProgress size={22} aria-hidden="true" />
      <Typography>{label}</Typography>
    </Box>
  );
}

interface FrontierActionsProps {
  action: FrontierAction;
  chapter: ChapterDto;
  heroName: string;
  isPending: boolean;
  pendingChoiceId: string | null;
  readingAids: boolean;
  /** Choix en grands caractères quand l'enfant lit seul. */
  largeText: boolean;
  protectedNames: ReadonlySet<string>;
  onAdvance: () => void;
  onChoose: (choiceId: string) => void;
}

/** Ce qui suit le dernier chapitre écrit : continuer, choisir, ou patienter. */
export function FrontierActions({
  action,
  chapter,
  heroName,
  isPending,
  pendingChoiceId,
  readingAids,
  largeText,
  protectedNames,
  onAdvance,
  onChoose,
}: FrontierActionsProps) {
  if (action === "waiting") {
    return <WritingStatus label="Le chapitre suivant est en cours d'écriture…" />;
  }

  if (action === "advance") {
    return isPending ? (
      <WritingStatus label="L'histoire s'écrit… le chapitre suivant arrive." />
    ) : (
      <Button
        variant="contained"
        size="large"
        endIcon={<ArrowForwardRoundedIcon />}
        onClick={onAdvance}
        sx={{ width: { xs: "100%", sm: "auto" }, minHeight: 56, px: 4, fontSize: "1.05rem" }}
      >
        Continuer l'histoire
      </Button>
    );
  }

  if (action !== "choose") {
    return null;
  }

  return (
    <Box component="section" aria-labelledby="choice-heading">
      <Typography id="choice-heading" variant="h3" sx={{ fontSize: "1.25rem", fontWeight: 700, mb: 2 }}>
        Que va faire {heroName} ?
      </Typography>
      <Stack spacing={1.5}>
        {chapter.choices.map((choice) => (
          <Button
            key={choice.id}
            variant="outlined"
            size="large"
            disabled={isPending}
            onClick={() => onChoose(choice.id)}
            endIcon={pendingChoiceId === choice.id ? <CircularProgress size={20} aria-hidden="true" /> : undefined}
            sx={{
              justifyContent: "space-between",
              textAlign: "left",
              minHeight: 56,
              py: 1.5,
              px: 2.5,
              fontSize: largeText ? "clamp(1.2rem, 1.1rem + 0.5vw, 1.4rem)" : "1.05rem",
              lineHeight: largeText ? 1.8 : 1.5,
              borderWidth: 2,
              "&:hover": { borderWidth: 2 },
              "&.Mui-disabled": { borderWidth: 2, opacity: pendingChoiceId === choice.id ? 1 : 0.5 },
            }}
          >
            <InlineReadingText text={choice.label} readingAids={readingAids} protectedNames={protectedNames} />
          </Button>
        ))}
      </Stack>
      {isPending && (
        <Box sx={{ mt: 2 }}>
          <WritingStatus label="L'histoire s'écrit… le chapitre suivant arrive." />
        </Box>
      )}
    </Box>
  );
}

interface EndingPanelProps {
  saveState: "saving" | "saved" | "error";
  saveError: string | null;
  onRetrySave: () => void;
  onReadAgain: () => void;
}

/** Fin de l'histoire : confirmation d'enregistrement local et suites possibles. */
export function EndingPanel({ saveState, saveError, onRetrySave, onReadAgain }: EndingPanelProps) {
  return (
    <Box component="section" aria-labelledby="ending-heading" sx={{ pt: 3, borderTop: 1, borderColor: "divider" }}>
      <Typography id="ending-heading" variant="h3" sx={{ fontSize: "1.75rem", fontWeight: 700, mb: 2 }}>
        Fin
      </Typography>

      {saveState === "saving" && <WritingStatus label="Enregistrement dans votre bibliothèque…" />}
      {saveState === "saved" && (
        <Box role="status" sx={{ display: "flex", alignItems: "center", gap: 1, minHeight: 48 }}>
          <CheckCircleRoundedIcon color="success" aria-hidden="true" />
          <Typography>Histoire enregistrée dans votre bibliothèque, sur cet appareil.</Typography>
        </Box>
      )}
      {saveState === "error" && (
        <Alert
          severity="warning"
          action={
            <Button color="inherit" onClick={onRetrySave}>
              Réessayer
            </Button>
          }
        >
          L'histoire n'a pas été enregistrée dans la bibliothèque. {saveError}
        </Alert>
      )}

      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} sx={{ mt: 3 }}>
        <Button variant="contained" size="large" component={RouterLink} to="/create">
          Créer une nouvelle histoire
        </Button>
        <Button variant="outlined" size="large" onClick={onReadAgain}>
          Relire depuis le début
        </Button>
        <Button size="large" component={RouterLink} to="/library">
          Ma bibliothèque
        </Button>
      </Stack>
    </Box>
  );
}
