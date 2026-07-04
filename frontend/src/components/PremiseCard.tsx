import Card from '@mui/material/Card';
import CardActionArea from '@mui/material/CardActionArea';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import Stack from '@mui/material/Stack';
import type { StoryPremiseDto } from '../api/types';

interface PremiseCardProps {
  premise: StoryPremiseDto;
  selected: boolean;
  onSelect: () => void;
}

/** Carte de sélection d'une proposition d'axe narratif (titre + accroche). */
export function PremiseCard({ premise, selected, onSelect }: PremiseCardProps) {
  return (
    <Card
      variant={selected ? 'elevation' : 'outlined'}
      elevation={selected ? 4 : 0}
      sx={{
        borderColor: selected ? 'primary.main' : 'divider',
        borderWidth: selected ? 2 : 1,
        borderStyle: 'solid',
      }}
    >
      <CardActionArea onClick={onSelect} sx={{ p: 2, minHeight: 48 }}>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <Stack spacing={0.5}>
            <Typography variant="subtitle1" component="span" fontWeight={600}>
              {premise.title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {premise.hook}
            </Typography>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
