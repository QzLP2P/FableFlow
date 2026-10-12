import { createTheme, type PaletteMode, type Theme } from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Palette {
    /** Deux couleurs alternées des aides à la lecture (contraste ≥ 4.5 sur le fond). */
    syllable: { a: string; b: string };
  }
  interface PaletteOptions {
    syllable?: { a: string; b: string };
  }
}

/**
 * Thème MUI mobile-first : lecture confortable, gros boutons de choix,
 * typographie responsive et palette rassurante adaptée à un public familial.
 * Supporte un mode clair et un mode sombre, tous deux réglés pour une lecture
 * prolongée (contrastes doux, jamais de noir/blanc pur).
 */
export function getAppTheme(mode: PaletteMode): Theme {
  const isLight = mode === 'light';

  return createTheme({
    palette: {
      mode,
      primary: {
        main: isLight ? '#5B3EA6' : '#B79CF2',
      },
      secondary: {
        main: '#F2994A',
      },
      background: {
        default: isLight ? '#FBF9FF' : '#181521',
        paper: isLight ? '#FFFFFF' : '#221F2E',
      },
      text: {
        primary: isLight ? '#241E33' : '#E7E3F0',
        secondary: isLight ? '#5A5470' : '#B9B3C9',
      },
      syllable: isLight ? { a: '#1C54B2', b: '#B3261E' } : { a: '#8EC5FF', b: '#FF9E8F' },
    },
    shape: {
      borderRadius: 16,
    },
    typography: {
      fontFamily: '"Segoe UI", Roboto, system-ui, sans-serif',
      h1: { fontSize: 'clamp(1.75rem, 5vw, 2.5rem)', fontWeight: 700 },
      h2: { fontSize: 'clamp(1.5rem, 4vw, 2rem)', fontWeight: 700 },
      body1: { fontSize: '1.05rem', lineHeight: 1.7 },
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            minHeight: 48,
            textTransform: 'none',
            fontWeight: 600,
          },
        },
      },
      MuiContainer: {
        defaultProps: {
          maxWidth: 'sm',
        },
      },
      MuiDialog: {
        styleOverrides: {
          // Pas de voile d'élévation en mode sombre : la surface reste background.paper, comme les cartes.
          paper: { backgroundImage: 'none' },
        },
      },
    },
  });
}

