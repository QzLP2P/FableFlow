import Typography from "@mui/material/Typography";

/** Rappelle où vivent les données : ce navigateur seulement, sans compte ni synchronisation. */
export function RetentionNotice({ subject }: { subject: "stories" | "profiles" }) {
  return (
    <Typography color="text.secondary" sx={{ maxWidth: "65ch" }}>
      {subject === "stories"
        ? "Les histoires terminées sont enregistrées automatiquement dans ce navigateur, sur cet appareil uniquement : pas de compte, pas de synchronisation. Effacer les données du navigateur les supprime, et certaines illustrations peuvent ne plus s'afficher avec le temps."
        : "Les profils sont enregistrés dans ce navigateur, sur cet appareil uniquement : pas de compte, pas de synchronisation. Modifier un profil ne change pas les histoires déjà écrites."}
    </Typography>
  );
}
