import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import LightModeIcon from '@mui/icons-material/LightMode';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import { useThemeMode } from '../theme/ThemeModeProvider';

/** Bouton de bascule entre lecture claire et sombre, adapté au tactile (>= 48px). */
export function ThemeToggleButton() {
  const { mode, toggleMode } = useThemeMode();
  const isLight = mode === 'light';

  return (
    <Tooltip title={isLight ? 'Passer en mode sombre' : 'Passer en mode clair'}>
      <IconButton
        onClick={toggleMode}
        color="inherit"
        aria-label={isLight ? 'Activer le mode sombre' : 'Activer le mode clair'}
        sx={{ width: 48, height: 48 }}
      >
        {isLight ? <DarkModeIcon /> : <LightModeIcon />}
      </IconButton>
    </Tooltip>
  );
}
