import Box from '@mui/material/Box';
import type { ReactNode } from 'react';
import { ThemeToggleButton } from './ThemeToggleButton';

interface AppLayoutProps {
  children: ReactNode;
}

/**
 * Habillage commun des pages : place le bouton clair/sombre en haut à droite,
 * accessible au pouce sur mobile et respectueux des zones de sécurité (safe-area).
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
          justifyContent: 'flex-end',
          px: { xs: 1, sm: 2 },
          pt: 'max(0.5rem, env(safe-area-inset-top))',
          bgcolor: 'background.default',
        }}
      >
        <ThemeToggleButton />
      </Box>
      {children}
    </Box>
  );
}
