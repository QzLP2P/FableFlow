import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import AutoStoriesRoundedIcon from "@mui/icons-material/AutoStoriesRounded";
import PersonAddAlt1RoundedIcon from "@mui/icons-material/PersonAddAlt1Rounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import FormControlLabel from "@mui/material/FormControlLabel";
import Stack from "@mui/material/Stack";
import Step from "@mui/material/Step";
import StepButton from "@mui/material/StepButton";
import Stepper from "@mui/material/Stepper";
import Typography from "@mui/material/Typography";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useReducer, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { startAdventure, toApiError } from "../api/apiClient";
import type { StartAdventureRequest } from "../api/types";
import {
  buildStartRequest,
  creatorReducer,
  customPremiseError,
  effectiveReadingAids,
  initialCreatorState,
  premiseRequest,
  type CreatorState,
  type CreatorStep,
} from "../components/creator/creatorState";
import { HeroForm, ReaderSettings } from "../components/creator/HeroForm";
import { PremiseStep } from "../components/creator/PremiseStep";
import { UniverseStep } from "../components/creator/UniverseStep";
import { describeHero, normalizeText, validateHeroDraft } from "../domain/hero";
import { adventureKey } from "../hooks/useAdventureReader";
import { useStoryPremises } from "../hooks/useCatalog";
import { useProfiles, useSaveProfile } from "../hooks/useLocalData";
import { describeStorageFailure } from "../storage/localData";
import { loadProfiles } from "../storage/profiles";
import { setStoryReadingAids } from "../storage/readingPreferences";

const STEPS: ReadonlyArray<{ label: string; heading: string }> = [
  { label: "Héros", heading: "Pour qui est cette histoire ?" },
  { label: "Univers", heading: "Dans quel univers ?" },
  { label: "Histoire", heading: "Comment commence-t-elle ?" },
];

const INVALID_FIELD_SELECTOR = '[aria-invalid="true"], [data-invalid="true"] input, [data-invalid="true"] button';

/** Préremplit le héros depuis `?profile=<id>` (lecture locale synchrone, sans effet). */
function initCreator(profileId: string | null): CreatorState {
  if (!profileId) {
    return initialCreatorState;
  }
  try {
    const profile = loadProfiles().profiles.find((p) => p.id === profileId);
    return profile ? creatorReducer(initialCreatorState, { type: "selectProfile", profile }) : initialCreatorState;
  } catch {
    return initialCreatorState;
  }
}

export function CreatePage() {
  const [searchParams] = useSearchParams();
  const [state, dispatch] = useReducer(creatorReducer, searchParams.get("profile"), initCreator);
  const [profileWarning, setProfileWarning] = useState<string | null>(null);
  const [showCustomError, setShowCustomError] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const profiles = useProfiles();
  const saveProfile = useSaveProfile();
  const headingRef = useRef<HTMLHeadingElement>(null);
  const stepContentRef = useRef<HTMLElement>(null);
  const [invalidAttempt, setInvalidAttempt] = useState(0);

  const request = useMemo(() => premiseRequest(state), [state]);
  const premises = useStoryPremises(state.themeId, request, state.premiseGeneration, state.step === 2);

  const start = useMutation({
    mutationFn: (startRequest: StartAdventureRequest) => startAdventure(startRequest),
    onSuccess: (adventure) => {
      queryClient.setQueryData(adventureKey(adventure.adventureId), adventure);
      setStoryReadingAids(adventure.adventureId, effectiveReadingAids(state));
      navigate(`/adventures/${adventure.adventureId}`);
    },
  });

  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    window.scrollTo({ top: 0 });
    headingRef.current?.focus({ preventScroll: true });
  }, [state.step]);

  // Après un « Continuer » refusé, amène le focus (et donc la vue) sur le premier champ en erreur.
  useEffect(() => {
    if (invalidAttempt > 0) {
      stepContentRef.current?.querySelector<HTMLElement>(INVALID_FIELD_SELECTOR)?.focus();
    }
  }, [invalidAttempt]);

  const heroErrors = state.showHeroErrors ? validateHeroDraft(state.hero) : {};
  const heroName = normalizeText(state.hero.name);
  const startRequest = buildStartRequest(state, premises.data);

  const continueFromHero = () => {
    dispatch({ type: "next" });
    if (Object.keys(validateHeroDraft(state.hero)).length > 0) {
      setInvalidAttempt((count) => count + 1);
      return;
    }
    if (!state.rememberProfile || state.hero.age === null) {
      return;
    }
    setProfileWarning(null);
    saveProfile.mutate(
      {
        id: state.profileId ?? undefined,
        name: heroName,
        age: state.hero.age,
        companions: state.hero.companions.map(normalizeText).filter(Boolean),
        interests: normalizeText(state.hero.interests),
        personality: normalizeText(state.hero.personality),
        appearance: normalizeText(state.hero.appearance),
        readerMode: state.readerMode,
        readingAids: state.readingAids,
      },
      {
        onSuccess: (profile) => dispatch({ type: "profileSaved", profileId: profile.id }),
        onError: (error) =>
          setProfileWarning(`Le profil n'a pas été mémorisé : ${describeStorageFailure(error)} L'histoire peut quand même être créée.`),
      },
    );
  };

  const continueFromUniverse = () => {
    dispatch({ type: "next" });
    if (state.themeId === null) {
      setInvalidAttempt((count) => count + 1);
    }
  };

  const handleStart = () => {
    if (state.premiseSelection.kind === "custom" && customPremiseError(state.customPremise)) {
      setShowCustomError(true);
      setInvalidAttempt((count) => count + 1);
      return;
    }
    if (startRequest) {
      start.mutate(startRequest);
    }
  };

  const savedProfiles = profiles.data?.profiles ?? [];

  return (
    <Container sx={{ py: { xs: 3, sm: 5 } }}>
      <Typography variant="h1" sx={{ fontSize: { xs: "1.75rem", sm: "2.25rem" }, mb: 3 }}>
        Créer une histoire
      </Typography>

      <Stepper nonLinear activeStep={state.step} sx={{ mb: 4 }}>
        {STEPS.map((step, index) => (
          <Step key={step.label} completed={index < state.step}>
            <StepButton
              onClick={() => dispatch({ type: "goToStep", step: index as CreatorStep })}
              disabled={index >= state.step || start.isPending}
              sx={{ minHeight: 48 }}
            >
              {step.label}
            </StepButton>
          </Step>
        ))}
      </Stepper>

      <Box component="section" ref={stepContentRef} aria-labelledby="creator-step-heading">
        <Typography
          id="creator-step-heading"
          ref={headingRef}
          tabIndex={-1}
          variant="h2"
          sx={{ fontSize: { xs: "1.35rem", sm: "1.6rem" }, mb: 3, "&:focus": { outline: "none" } }}
        >
          {STEPS[state.step].heading}
        </Typography>

        {state.step === 0 && (
          <Stack spacing={4}>
            {savedProfiles.length > 0 && (
              <Box role="group" aria-label="Profils enregistrés">
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {savedProfiles.map((profile) => (
                    <Chip
                      key={profile.id}
                      label={describeHero(profile.name, profile.age)}
                      color={state.profileId === profile.id ? "primary" : "default"}
                      variant={state.profileId === profile.id ? "filled" : "outlined"}
                      onClick={() => dispatch({ type: "selectProfile", profile })}
                      aria-pressed={state.profileId === profile.id}
                      sx={{ minHeight: 48, borderRadius: 6, fontSize: "1rem", px: 1 }}
                    />
                  ))}
                  <Chip
                    icon={<PersonAddAlt1RoundedIcon />}
                    label="Nouveau héros"
                    variant={state.profileId === null ? "filled" : "outlined"}
                    color={state.profileId === null ? "primary" : "default"}
                    onClick={() => dispatch({ type: "newHero" })}
                    aria-pressed={state.profileId === null}
                    sx={{ minHeight: 48, borderRadius: 6, fontSize: "1rem", px: 1 }}
                  />
                </Box>
              </Box>
            )}

            <HeroForm draft={state.hero} errors={heroErrors} onChange={(patch) => dispatch({ type: "updateHero", patch })} />

            <ReaderSettings
              readerMode={state.readerMode}
              readingAids={state.readingAids}
              onReaderModeChange={(value) => dispatch({ type: "setReaderMode", value })}
              onReadingAidsChange={(value) => dispatch({ type: "setReadingAids", value })}
            />

            <Box>
              <FormControlLabel
                sx={{ minHeight: 48 }}
                control={
                  <Checkbox
                    checked={state.rememberProfile}
                    onChange={(event) => dispatch({ type: "setRememberProfile", value: event.target.checked })}
                  />
                }
                label={state.profileId ? "Mettre à jour ce profil sur cet appareil" : "Mémoriser ce profil sur cet appareil"}
              />
              <Typography variant="body2" color="text.secondary" sx={{ maxWidth: "60ch" }}>
                Enregistré uniquement dans ce navigateur, sans compte. Modifiable ou supprimable depuis « Famille ».
              </Typography>
            </Box>

            {state.showHeroErrors && Object.keys(heroErrors).length > 0 && (
              <Alert severity="error" role="alert">
                Complétez les champs signalés pour continuer.
              </Alert>
            )}
          </Stack>
        )}

        {state.step === 1 && (
          <Stack spacing={3}>
            {profileWarning && (
              <Alert severity="warning" onClose={() => setProfileWarning(null)}>
                {profileWarning}
              </Alert>
            )}
            <UniverseStep
              themeId={state.themeId}
              length={state.length}
              showThemeError={state.showThemeError}
              onThemeChange={(themeId) => dispatch({ type: "selectTheme", themeId })}
              onLengthChange={(value) => dispatch({ type: "setLength", value })}
            />
          </Stack>
        )}

        {state.step === 2 && (
          <PremiseStep
            heroName={heroName}
            premises={premises}
            selection={state.premiseSelection}
            customPremise={state.customPremise}
            showCustomError={showCustomError}
            onSelect={(selection) => dispatch({ type: "selectPremise", selection })}
            onCustomPremiseChange={(value) => dispatch({ type: "setCustomPremise", value })}
            onMorePremises={() => dispatch({ type: "morePremises" })}
          />
        )}
      </Box>

      {start.isError && (
        <Alert severity="error" sx={{ mt: 3 }}>
          {toApiError(start.error).message} Vos choix sont conservés : vous pouvez relancer.
        </Alert>
      )}

      <Box
        sx={{
          mt: 4,
          display: "flex",
          flexDirection: { xs: "column-reverse", sm: "row" },
          justifyContent: "space-between",
          gap: 1.5,
        }}
      >
        {state.step > 0 ? (
          <Button
            size="large"
            startIcon={<ArrowBackRoundedIcon />}
            onClick={() => dispatch({ type: "back" })}
            disabled={start.isPending}
          >
            Retour
          </Button>
        ) : (
          <span />
        )}

        {state.step < 2 ? (
          <Button
            variant="contained"
            size="large"
            onClick={state.step === 0 ? continueFromHero : continueFromUniverse}
          >
            Continuer
          </Button>
        ) : (
          <Button
            variant="contained"
            size="large"
            onClick={handleStart}
            disabled={start.isPending || (state.premiseSelection.kind === "suggestion" && !startRequest)}
            startIcon={start.isPending ? <CircularProgress size={20} color="inherit" /> : <AutoStoriesRoundedIcon />}
          >
            {start.isPending ? "Écriture du premier chapitre…" : "Commencer l'histoire"}
          </Button>
        )}
      </Box>
      {start.isPending && (
        <Typography role="status" color="text.secondary" sx={{ mt: 2, textAlign: { sm: "right" } }}>
          Le premier chapitre arrive, cela prend quelques secondes.
        </Typography>
      )}
    </Container>
  );
}
