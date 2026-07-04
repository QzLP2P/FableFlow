import Stack from '@mui/material/Stack';
import Button from '@mui/material/Button';
import type { ChoiceDto } from '../api/types';

interface ChoiceButtonsProps {
  choices: ChoiceDto[];
  onChoose: (choiceId: string) => void;
  disabled?: boolean;
}

/**
 * Liste des choix proposés à l'utilisateur. Boutons pleine largeur, empilés,
 * avec une hauteur tactile confortable (>= 48px) pour une lecture mobile-first.
 */
export function ChoiceButtons({ choices, onChoose, disabled }: ChoiceButtonsProps) {
  return (
    <Stack spacing={1.5}>
      {choices.map((choice) => (
        <Button
          key={choice.id}
          variant="contained"
          size="large"
          fullWidth
          disabled={disabled}
          onClick={() => onChoose(choice.id)}
          sx={{ py: 1.5, justifyContent: 'flex-start', textAlign: 'left' }}
        >
          {choice.label}
        </Button>
      ))}
    </Stack>
  );
}
