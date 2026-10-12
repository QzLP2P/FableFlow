import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import { useState } from "react";
import type { IllustrationStatus } from "../../api/types";

interface ChapterIllustrationProps {
  status: IllustrationStatus;
  url: string | null;
  alt: string;
}

/**
 * Emplacement d'illustration au ratio fixe (aucun saut de mise en page) : attente, image prête
 * ou repli lisible si l'image a échoué ou a expiré. Affiché seulement si les images sont activées.
 */
export function ChapterIllustration({ status, url, alt }: ChapterIllustrationProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const showImage = status === "Ready" && url !== null && failedUrl !== url;
  const message =
    status === "Pending" ? "Illustration en préparation…" : showImage ? null : "Illustration indisponible";

  return (
    <Box
      sx={{
        position: "relative",
        aspectRatio: "4 / 3",
        width: "100%",
        borderRadius: "16px",
        overflow: "hidden",
        bgcolor: "action.hover",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      {showImage ? (
        <Box
          component="img"
          src={url}
          alt={alt}
          onError={() => setFailedUrl(url)}
          sx={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
        />
      ) : (
        <Typography variant="body2" color="text.secondary" role={status === "Pending" ? "status" : undefined}>
          {message}
        </Typography>
      )}
    </Box>
  );
}
