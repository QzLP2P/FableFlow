import Box from '@mui/material/Box';
import LinearProgress from '@mui/material/LinearProgress';
import Typography from '@mui/material/Typography';

interface ProgressIndicatorProps {
  currentSceneNumber: number;
  targetSceneCount: number;
}

/** Indique la progression de l'aventure (scène courante / durée cible). */
export function ProgressIndicator({ currentSceneNumber, targetSceneCount }: ProgressIndicatorProps) {
  const progress = Math.min(100, (currentSceneNumber / targetSceneCount) * 100);

  return (
    <Box sx={{ mb: 2 }}>
      <Typography variant="caption" color="text.secondary">
        Scène {currentSceneNumber} / {targetSceneCount}
      </Typography>
      <LinearProgress
        variant="determinate"
        value={progress}
        sx={{ height: 8, borderRadius: 4, mt: 0.5 }}
      />
    </Box>
  );
}
