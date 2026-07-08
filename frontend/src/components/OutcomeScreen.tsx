import AutoStoriesIcon from "@mui/icons-material/AutoStories";
import EmojiEventsIcon from "@mui/icons-material/EmojiEvents";
import SentimentDissatisfiedIcon from "@mui/icons-material/SentimentDissatisfied";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";
import type { AdventureStatus } from "../api/types";
import { SceneIllustration } from "./SceneIllustration";

interface OutcomeScreenProps {
  status: AdventureStatus;
  message: string | null;
  imageUrl: string | null;
  imageGenerationEnabled: boolean;
  onRestart: () => void;
}

const OUTCOME_CONFIG: Record<
  AdventureStatus,
  { label: string; icon: ReactNode; color: string }
> = {
  Won: {
    label: "Victoire !",
    icon: <EmojiEventsIcon fontSize="large" />,
    color: "success.main",
  },
  Lost: {
    label: "Fin difficile",
    icon: <SentimentDissatisfiedIcon fontSize="large" />,
    color: "error.main",
  },
  Completed: {
    label: "Fin de l\u2019histoire",
    icon: <AutoStoriesIcon fontSize="large" />,
    color: "text.primary",
  },
  InProgress: { label: "", icon: null, color: "text.primary" },
};

/** Écran de conclusion d'une aventure : victoire, défaite ou fin neutre. */
export function OutcomeScreen({
  status,
  message,
  imageUrl,
  imageGenerationEnabled,
  onRestart,
}: OutcomeScreenProps) {
  const config = OUTCOME_CONFIG[status];

  return (
    <Stack spacing={3} alignItems="center" textAlign="center" sx={{ py: 4 }}>
      <Stack alignItems="center" spacing={1} sx={{ color: config.color }}>
        {config.icon}
        <Typography variant="h5" component="h2" fontWeight={700}>
          {config.label}
        </Typography>
      </Stack>
      {message && (
        <Typography variant="body1" sx={{ maxWidth: "60ch" }}>
          {message}
        </Typography>
      )}
      <Stack sx={{ width: "100%", maxWidth: 480 }}>
        <SceneIllustration
          imageUrl={imageUrl}
          alt="Illustration de la conclusion de l'aventure"
          enabled={imageGenerationEnabled}
        />
      </Stack>
      <Button
        variant="contained"
        size="large"
        onClick={onRestart}
        sx={{ minWidth: 220 }}
      >
        Recommencer une aventure
      </Button>
    </Stack>
  );
}
