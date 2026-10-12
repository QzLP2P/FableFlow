import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { BookList } from "../components/library/BookList";
import { ConfirmDialog } from "../components/library/ConfirmDialog";
import { RetentionNotice } from "../components/library/RetentionNotice";
import { useDeleteBooks, useLibrary } from "../hooks/useLocalData";
import type { SavedBook } from "../storage/books";
import { describeStorageFailure } from "../storage/localData";

export function LibraryPage() {
  const library = useLibrary();
  const deleteBooks = useDeleteBooks();
  const [pendingDelete, setPendingDelete] = useState<SavedBook | null>(null);
  const invalidIds = library.data?.invalidIds ?? [];
  const books = library.data?.books ?? [];

  const confirmDelete = () => {
    if (!pendingDelete) {
      return;
    }
    deleteBooks.mutate([pendingDelete.id], { onSettled: () => setPendingDelete(null) });
  };

  return (
    <Container maxWidth="md" sx={{ py: { xs: 3, sm: 5 } }}>
      <Stack spacing={3}>
        <Box>
          <Typography variant="h1" sx={{ fontSize: { xs: "1.75rem", sm: "2.25rem" }, mb: 1.5 }}>
            Ma bibliothèque
          </Typography>
          <RetentionNotice subject="stories" />
        </Box>

        {deleteBooks.isError && (
          <Alert severity="error" onClose={() => deleteBooks.reset()}>
            La suppression a échoué. {describeStorageFailure(deleteBooks.error)}
          </Alert>
        )}

        {library.isPending && (
          <Stack spacing={2} aria-busy="true">
            {[0, 1, 2].map((key) => (
              <Skeleton key={key} variant="rounded" height={112} sx={{ borderRadius: "16px" }} />
            ))}
          </Stack>
        )}

        {library.isError && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" onClick={() => void library.refetch()}>
                Réessayer
              </Button>
            }
          >
            La bibliothèque ne peut pas être ouverte. {describeStorageFailure(library.error)}
          </Alert>
        )}

        {invalidIds.length > 0 && (
          <Alert
            severity="warning"
            action={
              <Button color="inherit" onClick={() => deleteBooks.mutate(invalidIds)} disabled={deleteBooks.isPending}>
                Retirer
              </Button>
            }
          >
            {invalidIds.length === 1
              ? "Une histoire enregistrée est illisible (données abîmées ou d'un format inconnu). Vous pouvez la retirer."
              : `${invalidIds.length} histoires enregistrées sont illisibles (données abîmées ou d'un format inconnu). Vous pouvez les retirer.`}
          </Alert>
        )}

        {library.isSuccess && books.length === 0 && (
          <Box sx={{ py: 4 }}>
            <Typography variant="h2" sx={{ fontSize: "1.35rem", mb: 1 }}>
              Aucune histoire pour l'instant
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3, maxWidth: "60ch" }}>
              Chaque histoire terminée rejoint cette bibliothèque, prête à être relue.
            </Typography>
            <Button variant="contained" size="large" component={RouterLink} to="/create">
              Créer une histoire
            </Button>
          </Box>
        )}

        {books.length > 0 && <BookList books={books} onDelete={setPendingDelete} />}
      </Stack>

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Supprimer cette histoire ?"
        description={
          pendingDelete
            ? `« ${pendingDelete.title} » sera retirée de cet appareil. Cette action est définitive.`
            : ""
        }
        confirmLabel="Supprimer"
        pending={deleteBooks.isPending}
        onConfirm={confirmDelete}
        onClose={() => setPendingDelete(null)}
      />
    </Container>
  );
}
