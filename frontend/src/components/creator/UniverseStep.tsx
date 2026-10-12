import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import FormControl from "@mui/material/FormControl";
import FormHelperText from "@mui/material/FormHelperText";
import FormLabel from "@mui/material/FormLabel";
import Skeleton from "@mui/material/Skeleton";
import Typography from "@mui/material/Typography";
import { useId } from "react";
import type { StoryLength } from "../../api/types";
import { useThemes } from "../../hooks/useCatalog";
import { ThemeArt } from "../art/ThemeArt";
import { RadioCard } from "./RadioCard";

interface UniverseStepProps {
  themeId: string | null;
  length: StoryLength;
  showThemeError: boolean;
  onThemeChange: (themeId: string) => void;
  onLengthChange: (length: StoryLength) => void;
}

const LENGTH_OPTIONS: ReadonlyArray<{ value: StoryLength; title: string; detail: string }> = [
  { value: "Short", title: "Courte", detail: "6 chapitres, 2 choix · environ 10 minutes" },
  { value: "Longer", title: "Plus longue", detail: "10 chapitres, 3 choix · environ 20 minutes" },
];

export function UniverseStep({ themeId, length, showThemeError, onThemeChange, onLengthChange }: UniverseStepProps) {
  const themes = useThemes();
  const themeName = useId();
  const lengthName = useId();
  const themeErrorId = useId();

  return (
    <Box sx={{ display: "grid", gap: 4 }}>
      <FormControl
        component="fieldset"
        error={showThemeError}
        aria-describedby={showThemeError ? themeErrorId : undefined}
        data-invalid={showThemeError ? "true" : undefined}
      >
        <FormLabel component="legend" sx={{ mb: 1.5 }}>
          Choisissez un univers
        </FormLabel>

        {showThemeError && (
          <FormHelperText id={themeErrorId} role="alert" sx={{ mx: 0, mt: -0.5, mb: 1.5, fontSize: "0.95rem" }}>
            Choisissez un univers pour continuer.
          </FormHelperText>
        )}

        {themes.isPending && (
          <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" } }} aria-busy="true">
            {[0, 1, 2, 3].map((key) => (
              <Skeleton key={key} variant="rounded" height={200} sx={{ borderRadius: "16px" }} />
            ))}
          </Box>
        )}

        {themes.isError && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" onClick={() => void themes.refetch()}>
                Réessayer
              </Button>
            }
          >
            Impossible de charger les univers. Vérifiez votre connexion puis réessayez.
          </Alert>
        )}

        {themes.data && (
          <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" } }}>
            {themes.data.map((theme) => (
              <RadioCard
                key={theme.id}
                name={themeName}
                value={theme.id}
                checked={theme.id === themeId}
                onChange={() => onThemeChange(theme.id)}
                media={<ThemeArt themeId={theme.id} />}
              >
                <Typography sx={{ fontWeight: 600 }}>{theme.displayName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {theme.description}
                </Typography>
              </RadioCard>
            ))}
          </Box>
        )}
      </FormControl>

      <FormControl component="fieldset">
        <FormLabel component="legend" sx={{ mb: 1.5 }}>
          Longueur de l'histoire
        </FormLabel>
        <Box sx={{ display: "grid", gap: 1.5, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" } }}>
          {LENGTH_OPTIONS.map((option) => (
            <RadioCard
              key={option.value}
              name={lengthName}
              value={option.value}
              checked={length === option.value}
              onChange={() => onLengthChange(option.value)}
            >
              <Typography sx={{ fontWeight: 600 }}>{option.title}</Typography>
              <Typography variant="body2" color="text.secondary">
                {option.detail}
              </Typography>
            </RadioCard>
          ))}
        </Box>
      </FormControl>
    </Box>
  );
}
