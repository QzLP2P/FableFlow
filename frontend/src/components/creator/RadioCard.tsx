import RadioButtonCheckedRoundedIcon from "@mui/icons-material/RadioButtonCheckedRounded";
import RadioButtonUncheckedRoundedIcon from "@mui/icons-material/RadioButtonUncheckedRounded";
import Box from "@mui/material/Box";
import type { ReactNode } from "react";

interface RadioCardProps {
  name: string;
  value: string;
  checked: boolean;
  onChange: () => void;
  media?: ReactNode;
  children: ReactNode;
}

/**
 * Option exclusive présentée en carte : vrai bouton radio natif (clavier, lecteurs d'écran),
 * état sélectionné signalé par l'icône et la bordure, pas seulement par la couleur.
 */
export function RadioCard({ name, value, checked, onChange, media, children }: RadioCardProps) {
  return (
    <Box
      component="label"
      sx={{
        position: "relative",
        display: "flex",
        flexDirection: "column",
        height: "100%",
        border: 2,
        borderColor: checked ? "primary.main" : "divider",
        borderRadius: "16px",
        overflow: "hidden",
        cursor: "pointer",
        bgcolor: checked ? "action.selected" : "background.paper",
        transition: "border-color 120ms ease",
        "&:hover": { borderColor: checked ? "primary.main" : "text.secondary" },
        "&:has(input:focus-visible)": { outline: "3px solid", outlineColor: "primary.main", outlineOffset: 2 },
      }}
    >
      <Box
        component="input"
        type="radio"
        name={name}
        value={value}
        checked={checked}
        onChange={onChange}
        sx={{ position: "absolute", opacity: 0, width: 1, height: 1, m: 0, pointerEvents: "none" }}
      />
      {media}
      <Box sx={{ display: "flex", gap: 1.5, alignItems: "flex-start", p: 2, minHeight: 48 }}>
        {checked ? (
          <RadioButtonCheckedRoundedIcon color="primary" aria-hidden="true" sx={{ mt: 0.25 }} />
        ) : (
          <RadioButtonUncheckedRoundedIcon color="action" aria-hidden="true" sx={{ mt: 0.25 }} />
        )}
        <Box sx={{ minWidth: 0, flex: 1 }}>{children}</Box>
      </Box>
    </Box>
  );
}
