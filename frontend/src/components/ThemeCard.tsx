import Card from '@mui/material/Card';
import CardActionArea from '@mui/material/CardActionArea';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import type { ThemeDto } from '../api/types';

interface ThemeCardProps {
  theme: ThemeDto;
  selected: boolean;
  onSelect: (themeId: string) => void;
}

/** Carte de sélection d'un thème. Grande zone cliquable, adaptée au tactile mobile. */
export function ThemeCard({ theme, selected, onSelect }: ThemeCardProps) {
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
      <CardActionArea onClick={() => onSelect(theme.id)} sx={{ p: 2, minHeight: 56 }}>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <Stack spacing={1}>
            <Typography variant="h6" component="span">
              {theme.displayName}
            </Typography>
            <Stack direction="row" spacing={1}>
              <Chip
                size="small"
                label={theme.audience === 'Child' ? 'Public : enfant' : 'Public : adulte'}
              />
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
