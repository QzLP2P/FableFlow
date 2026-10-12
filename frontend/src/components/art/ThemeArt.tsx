import Box from "@mui/material/Box";
import { useTheme } from "@mui/material/styles";

interface ThemeArtProps {
  themeId: string;
}

/**
 * Vignettes originales et décoratives (aucun personnage ni logo sous licence) : de simples
 * formes évoquant chaque univers. `aria-hidden` : le nom de l'univers est donné en texte.
 */
export function ThemeArt({ themeId }: ThemeArtProps) {
  const theme = useTheme();
  const isLight = theme.palette.mode === "light";
  const sky = isLight ? "#EFE9FF" : "#2C2740";
  const ink = isLight ? "#3B2A73" : "#D9CCFF";
  const accent = theme.palette.secondary.main;

  return (
    <Box
      component="svg"
      viewBox="0 0 160 100"
      aria-hidden="true"
      focusable="false"
      sx={{ display: "block", width: "100%", height: "auto" }}
    >
      <rect width="160" height="100" fill={sky} />
      {renderScene(themeId, ink, accent, isLight)}
    </Box>
  );
}

function renderScene(themeId: string, ink: string, accent: string, isLight: boolean) {
  const leaf = isLight ? "#3E8E5A" : "#7FD39B";
  switch (themeId) {
    case "pokemon":
      return (
        <g>
          <ellipse cx="80" cy="88" rx="70" ry="10" fill={leaf} opacity="0.35" />
          <path d="M58 76 Q52 46 80 40 Q108 46 102 76 Z" fill={accent} />
          <path d="M64 46 L56 22 L72 40 Z M96 46 L104 22 L88 40 Z" fill={accent} />
          <circle cx="71" cy="58" r="4" fill={ink} />
          <circle cx="89" cy="58" r="4" fill={ink} />
          <path d="M74 68 Q80 73 86 68" stroke={ink} strokeWidth="2.5" fill="none" strokeLinecap="round" />
          <path d="M118 30 L126 44 L120 44 L128 60 L112 40 L119 40 Z" fill={ink} />
        </g>
      );
    case "spidey":
      return (
        <g stroke={ink} strokeWidth="1.5" fill="none">
          <path d="M0 100 V70 H18 V54 H34 V76 H50 V60 H64 V100" fill={ink} opacity="0.2" stroke="none" />
          <path d="M96 100 V58 H112 V72 H128 V48 H144 V66 H160 V100" fill={ink} opacity="0.2" stroke="none" />
          <path d="M80 50 L80 0 M80 50 L40 10 M80 50 L120 10 M80 50 L30 50 M80 50 L130 50 M80 50 L48 86 M80 50 L112 86" />
          <path d="M66 50 Q72 40 80 36 Q90 40 94 50 Q88 62 80 64 Q70 60 66 50 Z" />
          <path d="M52 50 Q62 28 80 22 Q100 28 108 50 Q98 74 80 78 Q60 72 52 50 Z" />
          <circle cx="80" cy="50" r="6" fill={accent} stroke="none" />
        </g>
      );
    case "dinosaur":
      return (
        <g>
          <circle cx="132" cy="24" r="12" fill={accent} opacity="0.8" />
          <path d="M0 86 Q40 70 80 82 T160 80 V100 H0 Z" fill={leaf} opacity="0.45" />
          <path
            d="M30 78 Q34 56 58 54 Q70 36 88 34 Q100 34 102 44 Q100 50 92 50 Q86 52 84 60 Q100 62 112 74 Q126 70 138 76 Q120 82 108 80 L106 90 H98 L96 82 H68 L66 90 H58 L56 80 Q40 82 30 78 Z"
            fill={ink}
          />
          <circle cx="94" cy="41" r="2" fill={isLight ? "#FFFFFF" : "#181521"} />
        </g>
      );
    case "vet":
      return (
        <g>
          <path d="M80 82 C44 60 48 30 68 30 Q78 30 80 42 Q82 30 92 30 C112 30 116 60 80 82 Z" fill={accent} opacity="0.85" />
          <rect x="74" y="44" width="12" height="28" rx="3" fill={isLight ? "#FFFFFF" : "#181521"} />
          <rect x="66" y="52" width="28" height="12" rx="3" fill={isLight ? "#FFFFFF" : "#181521"} />
          <g fill={ink}>
            <ellipse cx="128" cy="76" rx="9" ry="7" />
            <circle cx="117" cy="64" r="4" />
            <circle cx="125" cy="59" r="4" />
            <circle cx="134" cy="60" r="4" />
            <circle cx="140" cy="67" r="4" />
          </g>
        </g>
      );
    default:
      return (
        <g>
          <path d="M40 30 Q60 24 80 32 V84 Q60 76 40 82 Z" fill={ink} opacity="0.85" />
          <path d="M120 30 Q100 24 80 32 V84 Q100 76 120 82 Z" fill={ink} opacity="0.6" />
          <path d="M126 14 L129 22 L137 22 L131 27 L133 35 L126 30 L119 35 L121 27 L115 22 L123 22 Z" fill={accent} />
        </g>
      );
  }
}
