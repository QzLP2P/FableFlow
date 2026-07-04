import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import RefreshIcon from '@mui/icons-material/Refresh';
import { PremiseCard } from './PremiseCard';
import type { StoryPremiseDto } from '../api/types';

interface PremiseSelectorProps {
  premises: StoryPremiseDto[] | undefined;
  isLoading: boolean;
  isFetching: boolean;
  isError: boolean;
  selectedIndex: number | null;
  onSelect: (index: number) => void;
  onRefresh: () => void;
}

/**
 * Propose plusieurs axes narratifs générés pour le thème choisi, avec un bouton pour en
 * régénérer d'autres. L'axe sélectionné devient le fil conducteur de toute l'aventure.
 */
export function PremiseSelector({
  premises,
  isLoading,
  isFetching,
  isError,
  selectedIndex,
  onSelect,
  onRefresh,
}: PremiseSelectorProps) {
  return (
    <Stack spacing={1.5}>
      <Stack direction="row" alignItems="center" justifyContent="space-between">
        <Typography variant="h2" component="h2">
          Axe de l'histoire
        </Typography>
        <Button
          size="small"
          startIcon={<RefreshIcon />}
          onClick={onRefresh}
          disabled={isLoading || isFetching}
        >
          Autres idées
        </Button>
      </Stack>

      <Typography variant="body2" color="text.secondary">
        Choisis l'idée qui te plaît le plus : elle guidera toute l'aventure.
      </Typography>

      {(isLoading || isFetching) && (
        <Stack alignItems="center" sx={{ py: 3 }}>
          <CircularProgress size={28} />
        </Stack>
      )}

      {isError && !isLoading && !isFetching && (
        <Alert severity="error">
          Impossible de générer des idées d'histoire. Réessaie dans un instant.
        </Alert>
      )}

      {!isLoading && !isFetching && !isError && premises && (
        <Stack spacing={1.5}>
          {premises.map((premise, index) => (
            <PremiseCard
              key={`${premise.title}-${index}`}
              premise={premise}
              selected={index === selectedIndex}
              onSelect={() => onSelect(index)}
            />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
