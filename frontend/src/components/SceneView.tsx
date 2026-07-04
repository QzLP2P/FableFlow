import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import type { SceneDto } from '../api/types';

interface SceneViewProps {
  scene: SceneDto;
}

/**
 * Affiche le texte narratif et l'illustration éventuelle d'une scène.
 * Largeur de lecture limitée et interlignage aéré pour une lecture confortable mobile-first.
 */
export function SceneView({ scene }: SceneViewProps) {
  return (
    <Box>
      {scene.imageUrl && (
        <Box
          component="img"
          src={scene.imageUrl}
          alt={`Illustration de la scène ${scene.sceneNumber}`}
          sx={{
            width: '100%',
            borderRadius: 3,
            mb: 2,
            display: 'block',
            aspectRatio: '1 / 1',
            objectFit: 'cover',
          }}
        />
      )}
      <Typography
        variant="body1"
        sx={{ whiteSpace: 'pre-line', maxWidth: '68ch' }}
      >
        {scene.text}
      </Typography>
    </Box>
  );
}
