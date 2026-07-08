import Box from '@mui/material/Box';
import Fade from '@mui/material/Fade';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

interface SceneIllustrationProps {
  /** URL de l'illustration, ou `null` tant qu'elle n'est pas encore prête. */
  imageUrl: string | null;
  /** Texte alternatif de l'image. */
  alt: string;
  /** Si `false`, la génération d'image est désactivée côté serveur : rien n'est affiché. */
  enabled: boolean;
}

/**
 * Affiche l'illustration d'une scène ou de l'issue d'une aventure, sous les choix ou le bouton de
 * fin : le texte (rapide) n'attend jamais l'image (plus lente à générer). Tant que l'image n'est pas
 * prête, un espace réservé de même taille évite tout saut de mise en page lorsqu'elle apparaît.
 */
export function SceneIllustration({ imageUrl, alt, enabled }: SceneIllustrationProps) {
  if (!enabled) {
    return null;
  }

  return (
    <Box
      sx={{
        width: '100%',
        borderRadius: 3,
        overflow: 'hidden',
        aspectRatio: '1 / 1',
        bgcolor: 'action.hover',
      }}
    >
      {imageUrl ? (
        <Fade in timeout={500}>
          <Box
            component="img"
            src={imageUrl}
            alt={alt}
            sx={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
          />
        </Fade>
      ) : (
        <Stack
          alignItems="center"
          justifyContent="center"
          spacing={1}
          sx={{ width: '100%', height: '100%' }}
        >
          <CircularProgress size={28} />
          <Typography variant="caption" color="text.secondary">
            Illustration en cours de création…
          </Typography>
        </Stack>
      )}
    </Box>
  );
}
