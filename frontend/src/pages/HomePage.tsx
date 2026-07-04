import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Container from '@mui/material/Container';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import { useThemes } from '../hooks/useThemes';
import { useStoryPremises } from '../hooks/useStoryPremises';
import { useStartAdventure } from '../hooks/useStartAdventure';
import { ThemeCard } from '../components/ThemeCard';
import { PremiseSelector } from '../components/PremiseSelector';
import { SceneCountSelector } from '../components/SceneCountSelector';

const MIN_SCENES = 3;
const MAX_SCENES = 12;
const DEFAULT_SCENES = 6;

/** Page d'accueil : choix du thème, de l'axe narratif et de la durée de l'aventure. */
export function HomePage() {
  const navigate = useNavigate();
  const { data: themes, isLoading, isError } = useThemes();
  const startAdventure = useStartAdventure();

  const [selectedThemeId, setSelectedThemeId] = useState<string | null>(null);
  const [selectedPremiseIndex, setSelectedPremiseIndex] = useState<number | null>(null);
  const [sceneCount, setSceneCount] = useState(DEFAULT_SCENES);

  const {
    data: premises,
    isLoading: isPremisesLoading,
    isFetching: isPremisesFetching,
    isError: isPremisesError,
    refetch: refetchPremises,
  } = useStoryPremises(selectedThemeId);

  const selectedPremise = selectedPremiseIndex !== null ? premises?.[selectedPremiseIndex] : null;

  const handleSelectTheme = (themeId: string) => {
    setSelectedThemeId(themeId);
    setSelectedPremiseIndex(null);
  };

  const handleStart = () => {
    if (!selectedThemeId || !selectedPremise) {
      return;
    }

    startAdventure.mutate(
      {
        themeId: selectedThemeId,
        sceneCount,
        narrativePremise: `${selectedPremise.title} — ${selectedPremise.hook}`,
      },
      {
        onSuccess: (adventure) => navigate(`/adventures/${adventure.adventureId}`),
      },
    );
  };

  return (
    <Container sx={{ py: { xs: 3, sm: 6 } }}>
      <Stack spacing={3}>
        <Stack spacing={1}>
          <Typography variant="h1" component="h1">
            Prêt pour l'aventure ?
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Choisis un thème et une durée pour démarrer ton aventure interactive.
          </Typography>
        </Stack>

        {isLoading && (
          <Stack alignItems="center" sx={{ py: 4 }}>
            <CircularProgress />
          </Stack>
        )}

        {isError && (
          <Alert severity="error">
            Impossible de charger les thèmes. Vérifie que l'API est démarrée.
          </Alert>
        )}

        {themes && (
          <Stack spacing={2}>
            <Typography variant="h2" component="h2">
              Thème
            </Typography>
            <Stack spacing={1.5}>
              {themes.map((theme) => (
                <ThemeCard
                  key={theme.id}
                  theme={theme}
                  selected={theme.id === selectedThemeId}
                  onSelect={handleSelectTheme}
                />
              ))}
            </Stack>
          </Stack>
        )}

        {selectedThemeId && (
          <PremiseSelector
            premises={premises}
            isLoading={isPremisesLoading}
            isFetching={isPremisesFetching}
            isError={isPremisesError}
            selectedIndex={selectedPremiseIndex}
            onSelect={setSelectedPremiseIndex}
            onRefresh={() => {
              setSelectedPremiseIndex(null);
              void refetchPremises();
            }}
          />
        )}

        <SceneCountSelector
          value={sceneCount}
          min={MIN_SCENES}
          max={MAX_SCENES}
          onChange={setSceneCount}
        />

        {startAdventure.isError && (
          <Alert severity="error">
            Impossible de démarrer l'aventure. Réessaie dans un instant.
          </Alert>
        )}

        <Button
          variant="contained"
          size="large"
          fullWidth
          disabled={!selectedThemeId || !selectedPremise || startAdventure.isPending}
          onClick={handleStart}
          sx={{ py: 1.5 }}
        >
          {startAdventure.isPending ? 'Préparation de l\u2019aventure…' : 'Commencer l\u2019aventure'}
        </Button>
      </Stack>
    </Container>
  );
}

