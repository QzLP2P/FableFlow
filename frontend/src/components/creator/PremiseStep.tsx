import AutorenewRoundedIcon from "@mui/icons-material/AutorenewRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControl from "@mui/material/FormControl";
import FormLabel from "@mui/material/FormLabel";
import Skeleton from "@mui/material/Skeleton";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type { UseQueryResult } from "@tanstack/react-query";
import { useId } from "react";
import { toApiError } from "../../api/apiClient";
import type { StoryPremiseDto } from "../../api/types";
import { MAX_PREMISE_LENGTH } from "../../domain/hero";
import { customPremiseError, PREMISE_COUNT, type PremiseSelection } from "./creatorState";
import { RadioCard } from "./RadioCard";

interface PremiseStepProps {
  heroName: string;
  premises: UseQueryResult<StoryPremiseDto[]>;
  selection: PremiseSelection;
  customPremise: string;
  showCustomError: boolean;
  onSelect: (selection: PremiseSelection) => void;
  onCustomPremiseChange: (value: string) => void;
  onMorePremises: () => void;
}

export function PremiseStep({
  heroName,
  premises,
  selection,
  customPremise,
  showCustomError,
  onSelect,
  onCustomPremiseChange,
  onMorePremises,
}: PremiseStepProps) {
  const name = useId();
  const isCustom = selection.kind === "custom";
  const customError = customPremise.length > MAX_PREMISE_LENGTH || showCustomError ? customPremiseError(customPremise) : null;

  return (
    <FormControl component="fieldset" sx={{ display: "grid", gap: 2 }}>
      <FormLabel component="legend" sx={{ mb: 1.5 }}>
        Choisissez le point de départ
      </FormLabel>

      {premises.isFetching && !premises.data && (
        <Box sx={{ display: "grid", gap: 1.5 }}>
          <Typography role="status" color="text.secondary">
            Nous imaginons des idées pour {heroName}…
          </Typography>
          {Array.from({ length: PREMISE_COUNT }, (_, key) => (
            <Skeleton key={key} variant="rounded" height={96} sx={{ borderRadius: "16px" }} />
          ))}
        </Box>
      )}

      {premises.isError && (
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={() => void premises.refetch()}>
              Réessayer
            </Button>
          }
        >
          {toApiError(premises.error).message} Vous pouvez aussi écrire votre propre idée ci-dessous.
        </Alert>
      )}

      {premises.data?.map((premise, index) => (
        <RadioCard
          key={`${premise.title}-${index}`}
          name={name}
          value={String(index)}
          checked={selection.kind === "suggestion" && selection.index === index}
          onChange={() => onSelect({ kind: "suggestion", index })}
        >
          <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1, mb: 0.5 }}>
            <Typography component="span" sx={{ fontWeight: 600 }}>
              {premise.title}
            </Typography>
            {index === 0 && <Chip label="Suggestion" size="small" color="primary" variant="outlined" />}
          </Box>
          <Typography variant="body2" color="text.secondary">
            {premise.hook}
          </Typography>
        </RadioCard>
      ))}

      {premises.data && (
        <Button
          onClick={onMorePremises}
          disabled={premises.isFetching}
          startIcon={<AutorenewRoundedIcon />}
          sx={{ justifySelf: "start" }}
        >
          Proposer d'autres idées
        </Button>
      )}

      <RadioCard name={name} value="custom" checked={isCustom} onChange={() => onSelect({ kind: "custom" })}>
        <Typography sx={{ fontWeight: 600 }}>Ma propre idée</Typography>
        <Typography variant="body2" color="text.secondary">
          Décrivez la situation de départ ; la suite reste une surprise.
        </Typography>
      </RadioCard>

      {isCustom && (
        <TextField
          label="Votre idée"
          placeholder={`${heroName || "Le héros"} trouve un œuf qui brille au fond du jardin…`}
          value={customPremise}
          onChange={(event) => onCustomPremiseChange(event.target.value)}
          error={Boolean(customError)}
          helperText={customError ?? `${customPremise.length}/${MAX_PREMISE_LENGTH}`}
          multiline
          minRows={3}
          autoFocus
          slotProps={{ htmlInput: { maxLength: MAX_PREMISE_LENGTH + 50 } }}
        />
      )}
    </FormControl>
  );
}
