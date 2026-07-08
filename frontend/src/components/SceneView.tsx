import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import type { SceneDto } from '../api/types';

interface SceneViewProps {
  scene: SceneDto;
}

/**
 * Affiche le texte narratif d'une scène. Largeur de lecture limitée et interlignage aéré pour une
 * lecture confortable mobile-first. L'illustration éventuelle est affichée séparément, sous les
 * choix (voir SceneIllustration), afin que le texte soit toujours disponible immédiatement sans
 * attendre la génération d'image, plus lente.
 */
export function SceneView({ scene }: SceneViewProps) {
  return (
    <Box>
      <Typography
        variant="body1"
        sx={{ whiteSpace: 'pre-line', maxWidth: '68ch' }}
      >
        {scene.text}
      </Typography>
    </Box>
  );
}
