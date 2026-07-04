import { useNavigate, useParams } from 'react-router-dom';
import Container from '@mui/material/Container';
import Stack from '@mui/material/Stack';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import { useAdventure } from '../hooks/useAdventure';
import { useMakeChoice } from '../hooks/useMakeChoice';
import { SceneView } from '../components/SceneView';
import { ChoiceButtons } from '../components/ChoiceButtons';
import { ProgressIndicator } from '../components/ProgressIndicator';
import { OutcomeScreen } from '../components/OutcomeScreen';

/** Page de jeu : affiche la scène courante et gère les choix de l'utilisateur jusqu'à la fin. */
export function AdventurePage() {
  const { adventureId } = useParams<{ adventureId: string }>();
  const navigate = useNavigate();
  const { data: adventure, isLoading, isError } = useAdventure(adventureId);
  const makeChoice = useMakeChoice(adventureId);

  if (isLoading) {
    return (
      <Container sx={{ py: 6 }}>
        <Stack alignItems="center">
          <CircularProgress />
        </Stack>
      </Container>
    );
  }

  if (isError || !adventure) {
    return (
      <Container sx={{ py: 6 }}>
        <Alert severity="error">Aventure introuvable ou API indisponible.</Alert>
      </Container>
    );
  }

  const isFinished = adventure.status !== 'InProgress';

  return (
    <Container sx={{ py: { xs: 3, sm: 6 } }}>
      <Stack spacing={3}>
        {!isFinished && adventure.currentScene && (
          <>
            <ProgressIndicator
              currentSceneNumber={adventure.currentSceneNumber}
              targetSceneCount={adventure.targetSceneCount}
            />
            <SceneView scene={adventure.currentScene} />

            {makeChoice.isError && (
              <Alert severity="error">
                Impossible d'appliquer ce choix. Réessaie dans un instant.
              </Alert>
            )}

            <ChoiceButtons
              choices={adventure.currentScene.choices}
              disabled={makeChoice.isPending}
              onChoose={(choiceId) => makeChoice.mutate(choiceId)}
            />
          </>
        )}

        {isFinished && (
          <OutcomeScreen
            status={adventure.status}
            message={adventure.outcomeMessage}
            onRestart={() => navigate('/')}
          />
        )}
      </Stack>
    </Container>
  );
}
