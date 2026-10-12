import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import { BookReader } from "../components/reader/BookReader";
import { WritingStatus } from "../components/reader/FrontierActions";
import { describeHero } from "../domain/hero";
import { libraryKey, useSavedBook } from "../hooks/useLocalData";
import { bookStore, type SavedBook } from "../storage/books";
import { describeStorageFailure } from "../storage/localData";
import { protectedWords } from "../text/syllabify";

export function BookPage() {
  const { bookId = "" } = useParams();
  const bookQuery = useSavedBook(bookId);

  if (bookQuery.isPending) {
    return (
      <Container sx={{ py: 6 }}>
        <WritingStatus label="Ouverture du livre…" />
      </Container>
    );
  }

  if (bookQuery.isError || !bookQuery.data) {
    return (
      <Container sx={{ py: 4 }}>
        <Typography variant="h1" sx={{ fontSize: "1.75rem", mb: 2 }}>
          Livre introuvable
        </Typography>
        <Typography sx={{ mb: 3, maxWidth: "65ch" }}>
          {bookQuery.isError
            ? describeStorageFailure(bookQuery.error)
            : "Cette histoire n'est pas (ou plus) enregistrée dans la bibliothèque de cet appareil."}
        </Typography>
        <Button variant="contained" size="large" component={RouterLink} to="/library">
          Retour à la bibliothèque
        </Button>
      </Container>
    );
  }

  return <SavedBookReader key={bookQuery.data.id} book={bookQuery.data} />;
}

/** Relecture d'un instantané enregistré : chemin figé, aucun choix rejouable, aucun appel réseau. */
export function SavedBookReader({ book }: { book: SavedBook }) {
  const queryClient = useQueryClient();
  const [chapterNumber, setChapterNumber] = useState(1);
  const [readingAids, setReadingAids] = useState(book.readingAids);
  const [aidsError, setAidsError] = useState<string | null>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const protectedNames = useMemo(() => protectedWords(book.hero.name, book.hero.companions), [book.hero]);
  const chapter = book.chapters.find((c) => c.number === chapterNumber) ?? book.chapters[0];
  const hasIllustrations = book.chapters.some((c) => c.illustrationUrl !== null);
  const isLast = chapter.number === book.chapters.length;

  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    const reduceMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
    window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
    headingRef.current?.focus({ preventScroll: true });
  }, [chapterNumber]);

  const handleReadingAids = (value: boolean) => {
    setReadingAids(value);
    setAidsError(null);
    bookStore
      .put({ ...book, readingAids: value })
      .then(() => queryClient.invalidateQueries({ queryKey: libraryKey }))
      .catch((error: unknown) =>
        setAidsError(`La préférence n'a pas été mémorisée pour la prochaine lecture. ${describeStorageFailure(error)}`),
      );
  };

  const subtitle = [describeHero(book.hero.name, book.hero.age), book.themeName].filter(Boolean).join(" · ");

  return (
    <BookReader
      title={book.title}
      subtitle={subtitle}
      chapter={{
        number: chapter.number,
        kind: chapter.kind,
        text: chapter.text,
        selectedChoiceLabel: chapter.selectedChoiceLabel,
        illustration: { status: chapter.illustrationUrl ? "Ready" : "Failed", url: chapter.illustrationUrl },
      }}
      totalChapters={book.chapters.length}
      readableChapters={book.chapters.length}
      onNavigate={setChapterNumber}
      readerMode={book.readerMode}
      readingAids={readingAids}
      onReadingAidsChange={handleReadingAids}
      protectedNames={protectedNames}
      showIllustrations={hasIllustrations}
      isFrontier={false}
      headingRef={headingRef}
      actions={
        aidsError || isLast ? (
          <Stack spacing={2}>
            {aidsError && <Alert severity="warning">{aidsError}</Alert>}
            {isLast && (
              <Box component="section" aria-labelledby="ending-heading" sx={{ pt: 3, borderTop: 1, borderColor: "divider" }}>
                <Typography id="ending-heading" variant="h3" sx={{ fontSize: "1.75rem", fontWeight: 700, mb: 2 }}>
                  Fin
                </Typography>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <Button variant="contained" size="large" onClick={() => setChapterNumber(1)}>
                    Relire depuis le début
                  </Button>
                  <Button variant="outlined" size="large" component={RouterLink} to="/library">
                    Ma bibliothèque
                  </Button>
                  <Button size="large" component={RouterLink} to="/create">
                    Créer une nouvelle histoire
                  </Button>
                </Stack>
              </Box>
            )}
          </Stack>
        ) : undefined
      }
    />
  );
}
