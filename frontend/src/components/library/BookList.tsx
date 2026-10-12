import DeleteOutlineRoundedIcon from "@mui/icons-material/DeleteOutlineRounded";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import IconButton from "@mui/material/IconButton";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";
import { describeHero } from "../../domain/hero";
import type { SavedBook } from "../../storage/books";
import { ThemeArt } from "../art/ThemeArt";

const dateFormatter = new Intl.DateTimeFormat("fr-FR", { day: "numeric", month: "long", year: "numeric" });

function formatSavedDate(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? "" : dateFormatter.format(date);
}

interface BookListProps {
  books: readonly SavedBook[];
  headingLevel?: "h2" | "h3";
  onDelete?: (book: SavedBook) => void;
}

/** Liste des livres enregistrés : vignette d'univers, titre, héros, date, lecture et suppression. */
export function BookList({ books, headingLevel = "h2", onDelete }: BookListProps) {
  return (
    <Box component="ul" sx={{ listStyle: "none", p: 0, m: 0, display: "grid", gap: 2 }}>
      {books.map((book) => {
        const meta = [
          book.themeName,
          `${book.chapters.length} chapitres`,
          book.readerMode === "ChildReader" ? "lue par l'enfant" : "à lire à voix haute",
        ]
          .filter(Boolean)
          .join(" · ");
        const date = formatSavedDate(book.savedAt);

        return (
          <Box
            component="li"
            key={book.id}
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "88px 1fr", sm: "128px 1fr auto" },
              gap: 2,
              alignItems: "center",
              p: 1.5,
              border: 1,
              borderColor: "divider",
              borderRadius: "16px",
              bgcolor: "background.paper",
            }}
          >
            <Box sx={{ borderRadius: "12px", overflow: "hidden", alignSelf: "start" }}>
              <ThemeArt themeId={book.themeId} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography component={headingLevel} sx={{ fontSize: "1.1rem", fontWeight: 700, lineHeight: 1.3 }}>
                {book.title}
              </Typography>
              <Typography color="text.secondary">{describeHero(book.hero.name, book.hero.age)}</Typography>
              <Typography variant="body2" color="text.secondary">
                {meta}
              </Typography>
              {date && (
                <Typography variant="body2" color="text.secondary">
                  Terminée le {date}
                </Typography>
              )}
            </Box>
            <Box
              sx={{
                gridColumn: { xs: "1 / -1", sm: "auto" },
                display: "flex",
                gap: 1,
                justifyContent: { xs: "stretch", sm: "flex-end" },
              }}
            >
              <Button
                variant="contained"
                component={RouterLink}
                to={`/library/${book.id}`}
                aria-label={`Lire « ${book.title} »`}
                sx={{ flex: { xs: 1, sm: "none" }, minHeight: 48 }}
              >
                Lire
              </Button>
              {onDelete && (
                <IconButton
                  aria-label={`Supprimer « ${book.title} »`}
                  onClick={() => onDelete(book)}
                  sx={{ width: 48, height: 48 }}
                >
                  <DeleteOutlineRoundedIcon />
                </IconButton>
              )}
            </Box>
          </Box>
        );
      })}
    </Box>
  );
}
