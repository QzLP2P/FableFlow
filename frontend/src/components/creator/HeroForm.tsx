import AddRoundedIcon from "@mui/icons-material/AddRounded";
import CloseRoundedIcon from "@mui/icons-material/CloseRounded";
import ExpandMoreRoundedIcon from "@mui/icons-material/ExpandMoreRounded";
import Accordion from "@mui/material/Accordion";
import AccordionDetails from "@mui/material/AccordionDetails";
import AccordionSummary from "@mui/material/AccordionSummary";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import FormHelperText from "@mui/material/FormHelperText";
import FormLabel from "@mui/material/FormLabel";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { useId, type ReactNode } from "react";
import type { ReaderMode } from "../../api/types";
import { HERO_AGES, HERO_LIMITS, type HeroDraft, type HeroFieldErrors } from "../../domain/hero";
import { InlineReadingText } from "../reader/ReadingText";
import { RadioCard } from "./RadioCard";

interface HeroFormProps {
  draft: HeroDraft;
  errors: HeroFieldErrors;
  onChange: (patch: Partial<HeroDraft>) => void;
}

/** Prénom et âge (obligatoires), détails facultatifs repliés par défaut. */
export function HeroForm({ draft, errors, onChange }: HeroFormProps) {
  const id = useId();
  const hasDetails =
    draft.companions.some(Boolean) || Boolean(draft.interests || draft.personality || draft.appearance);

  return (
    <Stack spacing={3}>
      <TextField
        id={`${id}-name`}
        label="Prénom du héros"
        value={draft.name}
        onChange={(event) => onChange({ name: event.target.value })}
        error={Boolean(errors.name)}
        helperText={errors.name}
        required
        autoComplete="off"
        slotProps={{ htmlInput: { maxLength: HERO_LIMITS.maxNameLength + 10 } }}
      />

      <FormControl component="fieldset" error={Boolean(errors.age)} data-invalid={errors.age ? "true" : undefined}>
        <FormLabel component="legend" required sx={{ mb: 1 }}>
          Âge
        </FormLabel>
        <ToggleButtonGroup
          id={`${id}-age`}
          exclusive
          value={draft.age}
          onChange={(_, value: number | null) => {
            if (value !== null) {
              onChange({ age: value });
            }
          }}
          aria-label="Âge du héros"
          sx={{
            flexWrap: "wrap",
            gap: 0.75,
            "& .MuiToggleButtonGroup-grouped": { border: 1, borderColor: "divider", borderRadius: "12px !important", m: 0 },
            "& .MuiToggleButtonGroup-grouped.Mui-selected": {
              bgcolor: "primary.main",
              borderColor: "primary.main",
              color: "primary.contrastText",
              "&:hover": { bgcolor: "primary.dark" },
            },
          }}
        >
          {HERO_AGES.map((age) => (
            <ToggleButton key={age} value={age} aria-label={`${age} ans`} sx={{ minWidth: 48, minHeight: 48, fontSize: "1rem" }}>
              {age}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
        <FormHelperText>{errors.age ?? "Le vocabulaire et la longueur des phrases s'adaptent à l'âge."}</FormHelperText>
      </FormControl>

      <Accordion
        defaultExpanded={hasDetails}
        disableGutters
        elevation={0}
        sx={{ border: 1, borderColor: "divider", borderRadius: "16px", "&::before": { display: "none" } }}
      >
        <AccordionSummary expandIcon={<ExpandMoreRoundedIcon />} sx={{ minHeight: 56 }}>
          <Typography sx={{ fontWeight: 600 }}>Personnaliser davantage (facultatif)</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Stack spacing={3}>
            <CompanionsField companions={draft.companions} error={errors.companions} onChange={(companions) => onChange({ companions })} />
            <DetailField
              label="Ce qu'il ou elle aime"
              placeholder="les étoiles, le foot, les chats…"
              value={draft.interests}
              error={errors.interests}
              onChange={(interests) => onChange({ interests })}
            />
            <DetailField
              label="Traits de caractère"
              placeholder="curieuse, un peu timide, très drôle…"
              value={draft.personality}
              error={errors.personality}
              onChange={(personality) => onChange({ personality })}
            />
            <DetailField
              label="Apparence"
              placeholder="cheveux bouclés, lunettes rouges…"
              value={draft.appearance}
              error={errors.appearance}
              onChange={(appearance) => onChange({ appearance })}
              helper="Utile pour les illustrations."
            />
          </Stack>
        </AccordionDetails>
      </Accordion>
    </Stack>
  );
}

function DetailField({
  label,
  placeholder,
  value,
  error,
  helper,
  onChange,
}: {
  label: string;
  placeholder: string;
  value: string;
  error?: string;
  helper?: ReactNode;
  onChange: (value: string) => void;
}) {
  return (
    <TextField
      label={label}
      placeholder={placeholder}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      error={Boolean(error)}
      helperText={error ?? helper ?? `${value.length}/${HERO_LIMITS.maxDetailLength}`}
      multiline
      minRows={1}
      slotProps={{ htmlInput: { maxLength: HERO_LIMITS.maxDetailLength } }}
    />
  );
}

function CompanionsField({
  companions,
  error,
  onChange,
}: {
  companions: string[];
  error?: string;
  onChange: (companions: string[]) => void;
}) {
  const list = companions.length > 0 ? companions : [""];
  const update = (index: number, value: string) => onChange(list.map((c, i) => (i === index ? value : c)));

  return (
    <FormControl component="fieldset" error={Boolean(error)}>
      <FormLabel component="legend" sx={{ mb: 1 }}>
        Compagnons (jusqu'à {HERO_LIMITS.maxCompanions})
      </FormLabel>
      <Stack spacing={1.5}>
        {list.map((companion, index) => (
          <Box key={index} sx={{ display: "flex", gap: 1, alignItems: "center" }}>
            <TextField
              fullWidth
              label={`Compagnon ${index + 1}`}
              placeholder="un doudou, une petite sœur, un chien…"
              value={companion}
              onChange={(event) => update(index, event.target.value)}
              slotProps={{ htmlInput: { maxLength: HERO_LIMITS.maxCompanionLength } }}
            />
            {list.length > 1 && (
              <IconButton
                aria-label={`Retirer le compagnon ${index + 1}`}
                onClick={() => onChange(list.filter((_, i) => i !== index))}
                sx={{ width: 48, height: 48 }}
              >
                <CloseRoundedIcon />
              </IconButton>
            )}
          </Box>
        ))}
      </Stack>
      {list.length < HERO_LIMITS.maxCompanions && (
        <Button startIcon={<AddRoundedIcon />} onClick={() => onChange([...list, ""])} sx={{ alignSelf: "flex-start", mt: 1 }}>
          Ajouter un compagnon
        </Button>
      )}
      {error && <FormHelperText>{error}</FormHelperText>}
    </FormControl>
  );
}

interface ReaderSettingsProps {
  readerMode: ReaderMode;
  readingAids: boolean;
  onReaderModeChange: (value: ReaderMode) => void;
  onReadingAidsChange: (value: boolean) => void;
}

/** Qui lit l'histoire ; les aides à la lecture ne concernent que l'enfant lecteur. */
export function ReaderSettings({ readerMode, readingAids, onReaderModeChange, onReadingAidsChange }: ReaderSettingsProps) {
  const name = useId();

  return (
    <FormControl component="fieldset">
      <FormLabel component="legend" sx={{ mb: 1 }}>
        Qui lit l'histoire ?
      </FormLabel>
      <Box sx={{ display: "grid", gap: 1.5, gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" } }}>
        <RadioCard name={name} value="AdultReadAloud" checked={readerMode === "AdultReadAloud"} onChange={() => onReaderModeChange("AdultReadAloud")}>
          <Typography sx={{ fontWeight: 600 }}>Un adulte, à voix haute</Typography>
          <Typography variant="body2" color="text.secondary">
            Des phrases riches, à partager ensemble.
          </Typography>
        </RadioCard>
        <RadioCard name={name} value="ChildReader" checked={readerMode === "ChildReader"} onChange={() => onReaderModeChange("ChildReader")}>
          <Typography sx={{ fontWeight: 600 }}>L'enfant, tout seul</Typography>
          <Typography variant="body2" color="text.secondary">
            Un texte plus simple, en plus grands caractères.
          </Typography>
        </RadioCard>
      </Box>

      {readerMode === "ChildReader" && (
        <Box sx={{ mt: 2 }}>
          <FormControlLabel
            sx={{ minHeight: 48 }}
            control={<Checkbox checked={readingAids} onChange={(event) => onReadingAidsChange(event.target.checked)} />}
            label="Aides à la lecture"
          />
          <Typography variant="body2" color="text.secondary" sx={{ maxWidth: "60ch" }}>
            Chaque syllabe écrite alterne deux couleurs et reçoit un arc, par exemple{" "}
            <Box component="span" sx={{ fontSize: "1.1rem", color: "text.primary" }}>
              <InlineReadingText text="le papillon" readingAids />
            </Box>
            . Les prénoms et quelques mots irréguliers restent entiers. Modifiable à tout moment pendant la lecture.
          </Typography>
        </Box>
      )}
    </FormControl>
  );
}
