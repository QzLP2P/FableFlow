import FamilyRestroomRoundedIcon from "@mui/icons-material/FamilyRestroomRounded";
import LocalLibraryRoundedIcon from "@mui/icons-material/LocalLibraryRounded";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";
import { Link as RouterLink, useMatch } from "react-router-dom";
import { ThemeToggleButton } from "./ThemeToggleButton";

interface AppLayoutProps {
  children: ReactNode;
}

/**
 * Habillage commun des pages : barre supérieure sticky avec le nom de l'application
 * (lien de retour vers l'accueil), la bibliothèque, la famille et le bouton clair/sombre,
 * accessibles au pouce sur mobile et respectueux des zones de sécurité (safe-area).
 */
export function AppLayout({ children }: AppLayoutProps) {
  return (
    <Box sx={{ minHeight: "100dvh" }}>
      <Box
        sx={{
          position: "sticky",
          top: 0,
          zIndex: 1,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          px: { xs: 1, sm: 2 },
          py: 0.5,
          pt: "max(0.5rem, env(safe-area-inset-top))",
          bgcolor: "background.default",
        }}
      >
        <Typography
          component={RouterLink}
          to="/"
          variant="h6"
          sx={{
            fontWeight: 700,
            color: "text.primary",
            textDecoration: "none",
            px: 1,
            minHeight: 48,
            display: "flex",
            alignItems: "center",
          }}
        >
          FableFlow
        </Typography>
          <Box component="nav" aria-label="Navigation principale" sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
            <NavItem to="/library" label="Bibliothèque" icon={<LocalLibraryRoundedIcon />} />
            <NavItem to="/family" label="Famille" icon={<FamilyRestroomRoundedIcon />} />
            <ThemeToggleButton />
          </Box>
      </Box>
      {children}
    </Box>
  );
}

function NavItem({ to, label, icon }: { to: string; label: string; icon: ReactNode }) {
  const active = useMatch({ path: to, end: false }) !== null;

  return (
    <Button
      component={RouterLink}
      to={to}
      aria-label={label}
      aria-current={active ? "page" : undefined}
      color="inherit"
      startIcon={icon}
      sx={{
          minWidth: 48,
          minHeight: 48,
          px: { xs: 1.5, sm: 2 },
          color: active ? "primary.main" : "text.primary",
          fontWeight: active ? 700 : 500,
          "& .MuiButton-startIcon": { mr: { xs: 0, sm: 1 }, ml: { xs: 0, sm: -0.5 } },
      }}
    >
      <Box component="span" sx={{ display: { xs: "none", sm: "inline" } }}>
          {label}
      </Box>
    </Button>
  );
}
