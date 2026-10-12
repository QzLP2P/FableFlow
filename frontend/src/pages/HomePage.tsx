import AddRoundedIcon from "@mui/icons-material/AddRounded";
import AutoStoriesRoundedIcon from "@mui/icons-material/AutoStoriesRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";
import { BookList } from "../components/library/BookList";
import { describeHero } from "../domain/hero";
import { useLibrary, useProfiles } from "../hooks/useLocalData";
import { describeStorageFailure } from "../storage/localData";

const RECENT_BOOKS = 3;

/** Accès direct aux profils et aux histoires enregistrées sur cet appareil. */
export function HomePage() {
  const profiles = useProfiles();
  const library = useLibrary();
  const savedProfiles = profiles.data?.profiles ?? [];
  const books = library.data?.books ?? [];
  const isReturning = savedProfiles.length > 0 || books.length > 0;
  // Évite d'afficher l'accueil « première visite » le temps de lire les données locales.
  const isSettled = !profiles.isPending && !library.isPending;

  return (
    <Container maxWidth="md" sx={{ py: { xs: 3, sm: 6 } }}>
      <Stack spacing={5}>
        {!isSettled ? (
          <Box aria-hidden="true">
            <Skeleton variant="text" sx={{ fontSize: { xs: "1.85rem", sm: "2.5rem" }, maxWidth: "20ch", mb: 1.5 }} />
            <Skeleton variant="text" sx={{ fontSize: "1.1rem", maxWidth: "50ch", mb: 3 }} />
            <Skeleton variant="rounded" width={200} height={48} sx={{ borderRadius: "16px" }} />
          </Box>
        ) : (
          <Box component="section" aria-labelledby="home-heading">
            <Typography id="home-heading" variant="h1" sx={{ fontSize: { xs: "1.85rem", sm: "2.5rem" }, mb: 1.5, maxWidth: "20ch" }}>
              {isReturning ? "Une nouvelle histoire ?" : "Une histoire dont votre enfant est le héros"}
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3, maxWidth: "60ch", fontSize: "1.1rem" }}>
              {isReturning
                ? "Choisissez le héros, puis l'univers : le premier chapitre arrive en quelques secondes."
                : "Son prénom, son âge, un univers : FableFlow écrit une histoire en chapitres à lire ensemble à voix haute, ou qu'il ou elle lit seul, avec quelques choix en chemin."}
            </Typography>

            {savedProfiles.length > 0 ? (
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5 }}>
                {savedProfiles.slice(0, 4).map((profile) => (
                  <Button
                    key={profile.id}
                    variant="contained"
                    size="large"
                    component={RouterLink}
                    to={`/create?profile=${encodeURIComponent(profile.id)}`}
                    startIcon={<AutoStoriesRoundedIcon />}
                  >
                    Pour {describeHero(profile.name, profile.age)}
                  </Button>
                ))}
                <Button variant="outlined" size="large" component={RouterLink} to="/create" startIcon={<AddRoundedIcon />}>
                  Autre héros
                </Button>
              </Box>
            ) : (
              <Button variant="contained" size="large" component={RouterLink} to="/create" startIcon={<AutoStoriesRoundedIcon />}>
                Créer une histoire
              </Button>
            )}

            {savedProfiles.length > 0 && (
              <Button component={RouterLink} to="/family" sx={{ mt: 1.5, ml: -1 }}>
                Gérer les profils
              </Button>
            )}
          </Box>
        )}

        {library.isPending && <Skeleton variant="rounded" height={112} sx={{ borderRadius: "16px" }} aria-hidden="true" />}

        {library.isError && (
          <Alert severity="warning">
            Les histoires enregistrées ne peuvent pas être affichées. {describeStorageFailure(library.error)}
          </Alert>
        )}

        {books.length > 0 && (
          <Box component="section" aria-labelledby="recent-heading">
            <Box sx={{ display: "flex", alignItems: "baseline", justifyContent: "space-between", gap: 2, mb: 2, flexWrap: "wrap" }}>
              <Typography id="recent-heading" variant="h2" sx={{ fontSize: "1.35rem", fontWeight: 700 }}>
                À relire
              </Typography>
              <Button component={RouterLink} to="/library" sx={{ mr: -1 }}>
                Toute la bibliothèque ({books.length})
              </Button>
            </Box>
            <BookList books={books.slice(0, RECENT_BOOKS)} headingLevel="h3" />
          </Box>
        )}
      </Stack>
    </Container>
  );
}
