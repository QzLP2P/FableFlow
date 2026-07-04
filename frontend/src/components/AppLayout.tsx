import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { ThemeToggleButton } from './ThemeToggleButton';

interface AppLayoutProps {
  children: ReactNode;
}

/**
 * Habillage commun des pages : barre supérieure sticky avec le nom de l'application
 * (lien de retour vers l'accueil, toujours visible) à gauche et le bouton clair/sombre
 * à droite, accessible au pouce sur mobile et respectueux des zones de sécurité (safe-area).
 */
export function AppLayout({ children }: AppLayoutProps) {
  return (
    <Box sx={{ minHeight: '100dvh' }}>
      <Box
        sx={{
          position: 'sticky',
          top: 0,
          zIndex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          px: { xs: 1, sm: 2 },
          py: 0.5,
          pt: 'max(0.5rem, env(safe-area-inset-top))',
          bgcolor: 'background.default',
        }}
      >
        <Typography
          component={RouterLink}
          to="/"
          variant="h6"
          sx={{
            fontWeight: 700,
            color: 'text.primary',
            textDecoration: 'none',
            px: 1,
            minHeight: 48,
            display: 'flex',
            alignItems: 'center',
          }}
        >
          FableFlow
        </Typography>
        <ThemeToggleButton />
      </Box>
      {children}
    </Box>
  );
}
