import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Slider from '@mui/material/Slider';

interface SceneCountSelectorProps {
  value: number;
  min: number;
  max: number;
  onChange: (value: number) => void;
}

/** Sélecteur de la durée de l'aventure (nombre de scènes). */
export function SceneCountSelector({ value, min, max, onChange }: SceneCountSelectorProps) {
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle1" fontWeight={600}>
        Durée de l'aventure : {value} scènes
      </Typography>
      <Slider
        value={value}
        min={min}
        max={max}
        step={1}
        marks
        valueLabelDisplay="auto"
        onChange={(_, newValue) => onChange(newValue as number)}
        aria-label="Nombre de scènes"
        sx={{ py: 2 }}
      />
    </Stack>
  );
}
