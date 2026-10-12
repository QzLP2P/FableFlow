import AddRoundedIcon from "@mui/icons-material/AddRounded";
import DeleteOutlineRoundedIcon from "@mui/icons-material/DeleteOutlineRounded";
import EditRoundedIcon from "@mui/icons-material/EditRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useTheme } from "@mui/material/styles";
import { useId, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import type { ReaderMode } from "../api/types";
import { heroDraftFromProfile } from "../components/creator/creatorState";
import { HeroForm, ReaderSettings } from "../components/creator/HeroForm";
import { ConfirmDialog } from "../components/library/ConfirmDialog";
import { RetentionNotice } from "../components/library/RetentionNotice";
import { describeHero, emptyHeroDraft, isHeroDraftValid, normalizeText, validateHeroDraft, type HeroDraft } from "../domain/hero";
import { useDeleteProfile, useDiscardInvalidProfiles, useProfiles, useSaveProfile } from "../hooks/useLocalData";
import { describeStorageFailure } from "../storage/localData";
import type { StoredProfile } from "../storage/profiles";

interface EditorState {
  id: string | null;
  draft: HeroDraft;
  readerMode: ReaderMode;
  readingAids: boolean;
}

export function FamilyPage() {
  const profiles = useProfiles();
  const deleteProfile = useDeleteProfile();
  const discardInvalid = useDiscardInvalidProfiles();
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [pendingDelete, setPendingDelete] = useState<StoredProfile | null>(null);
  const list = profiles.data?.profiles ?? [];
  const invalidCount = profiles.data?.invalidCount ?? 0;

  const openEditor = (profile: StoredProfile | null) =>
    setEditor(
      profile
        ? { id: profile.id, draft: heroDraftFromProfile(profile), readerMode: profile.readerMode, readingAids: profile.readingAids }
        : { id: null, draft: emptyHeroDraft, readerMode: "AdultReadAloud", readingAids: true },
    );

  return (
    <Container sx={{ py: { xs: 3, sm: 5 } }}>
      <Stack spacing={3}>
        <Box>
          <Typography variant="h1" sx={{ fontSize: { xs: "1.75rem", sm: "2.25rem" }, mb: 1.5 }}>
            Famille
          </Typography>
          <RetentionNotice subject="profiles" />
        </Box>

        {profiles.isError && (
          <Alert
            severity="error"
            action={
              <Button color="inherit" onClick={() => void profiles.refetch()}>
                Réessayer
              </Button>
            }
          >
            Les profils ne peuvent pas être lus. {describeStorageFailure(profiles.error)}
          </Alert>
        )}

        {(deleteProfile.isError || discardInvalid.isError) && (
          <Alert
            severity="error"
            onClose={() => {
              deleteProfile.reset();
              discardInvalid.reset();
            }}
          >
            La suppression a échoué. {describeStorageFailure(deleteProfile.error ?? discardInvalid.error)}
          </Alert>
        )}

        {invalidCount > 0 && (
          <Alert
            severity="warning"
            action={
              <Button color="inherit" onClick={() => discardInvalid.mutate()} disabled={discardInvalid.isPending}>
                Retirer
              </Button>
            }
          >
            {invalidCount === 1
              ? "Un profil enregistré est illisible et a été ignoré. Vous pouvez le retirer."
              : `${invalidCount} profils enregistrés sont illisibles et ont été ignorés. Vous pouvez les retirer.`}
          </Alert>
        )}

        {profiles.isSuccess && list.length === 0 && (
          <Typography color="text.secondary">
            Aucun profil pour l'instant. Ajoutez-en un ici, ou cochez « Mémoriser ce profil » en créant une histoire.
          </Typography>
        )}

        {list.length > 0 && (
          <Box component="ul" sx={{ listStyle: "none", p: 0, m: 0, display: "grid", gap: 1.5 }}>
            {list.map((profile) => (
              <Box
                component="li"
                key={profile.id}
                sx={{
                  display: "flex",
                  flexWrap: "wrap",
                  alignItems: "center",
                  gap: 1.5,
                  p: 2,
                  border: 1,
                  borderColor: "divider",
                  borderRadius: "16px",
                  bgcolor: "background.paper",
                }}
              >
                <Box sx={{ flex: "1 1 12rem", minWidth: 0 }}>
                  <Typography component="h2" sx={{ fontSize: "1.1rem", fontWeight: 700 }}>
                    {describeHero(profile.name, profile.age)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {profile.readerMode === "ChildReader"
                      ? `Lit seul${profile.readingAids ? ", avec les aides à la lecture" : ""}`
                      : "Histoires lues à voix haute"}
                    {profile.companions.length > 0 ? ` · avec ${profile.companions.join(", ")}` : ""}
                  </Typography>
                </Box>
                <Box sx={{ display: "flex", gap: 0.5, alignItems: "center" }}>
                  <Button variant="contained" component={RouterLink} to={`/create?profile=${encodeURIComponent(profile.id)}`}>
                    Nouvelle histoire
                  </Button>
                  <IconButton aria-label={`Modifier le profil de ${profile.name}`} onClick={() => openEditor(profile)} sx={{ width: 48, height: 48 }}>
                    <EditRoundedIcon />
                  </IconButton>
                  <IconButton
                    aria-label={`Supprimer le profil de ${profile.name}`}
                    onClick={() => setPendingDelete(profile)}
                    sx={{ width: 48, height: 48 }}
                  >
                    <DeleteOutlineRoundedIcon />
                  </IconButton>
                </Box>
              </Box>
            ))}
          </Box>
        )}

        {!profiles.isError && (
          <Button startIcon={<AddRoundedIcon />} variant="outlined" size="large" onClick={() => openEditor(null)} sx={{ alignSelf: "flex-start" }}>
            Ajouter un profil
          </Button>
        )}
      </Stack>

      {editor && <ProfileEditor key={editor.id ?? "new"} initial={editor} onClose={() => setEditor(null)} />}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Supprimer ce profil ?"
        description={
          pendingDelete
            ? `Le profil de ${pendingDelete.name} sera retiré de cet appareil. Les histoires déjà enregistrées restent dans la bibliothèque.`
            : ""
        }
        confirmLabel="Supprimer"
        pending={deleteProfile.isPending}
        onConfirm={() => {
          if (pendingDelete) {
            deleteProfile.mutate(pendingDelete.id, { onSettled: () => setPendingDelete(null) });
          }
        }}
        onClose={() => setPendingDelete(null)}
      />
    </Container>
  );
}

function ProfileEditor({ initial, onClose }: { initial: EditorState; onClose: () => void }) {
  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.down("sm"));
  const titleId = useId();
  const saveProfile = useSaveProfile();
  const [state, setState] = useState(initial);
  const [showErrors, setShowErrors] = useState(false);
  const errors = showErrors ? validateHeroDraft(state.draft) : {};

  const save = () => {
    if (!isHeroDraftValid(state.draft) || state.draft.age === null) {
      setShowErrors(true);
      return;
    }
    saveProfile.mutate(
      {
        id: state.id ?? undefined,
        name: normalizeText(state.draft.name),
        age: state.draft.age,
        companions: state.draft.companions.map(normalizeText).filter(Boolean),
        interests: normalizeText(state.draft.interests),
        personality: normalizeText(state.draft.personality),
        appearance: normalizeText(state.draft.appearance),
        readerMode: state.readerMode,
        readingAids: state.readingAids,
      },
      { onSuccess: onClose },
    );
  };

  return (
    <Dialog open fullScreen={fullScreen} fullWidth maxWidth="sm" onClose={saveProfile.isPending ? undefined : onClose} aria-labelledby={titleId}>
      <DialogTitle id={titleId}>{initial.id ? "Modifier le profil" : "Nouveau profil"}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={4} sx={{ pt: 1 }}>
          <HeroForm draft={state.draft} errors={errors} onChange={(patch) => setState((s) => ({ ...s, draft: { ...s.draft, ...patch } }))} />
          <ReaderSettings
            readerMode={state.readerMode}
            readingAids={state.readingAids}
            onReaderModeChange={(readerMode) => setState((s) => ({ ...s, readerMode }))}
            onReadingAidsChange={(readingAids) => setState((s) => ({ ...s, readingAids }))}
          />
          {saveProfile.isError && (
            <Alert severity="error">Le profil n'a pas été enregistré. {describeStorageFailure(saveProfile.error)}</Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2, gap: 1 }}>
        <Button onClick={onClose} disabled={saveProfile.isPending} size="large">
          Annuler
        </Button>
        <Button onClick={save} disabled={saveProfile.isPending} variant="contained" size="large">
          Enregistrer
        </Button>
      </DialogActions>
    </Dialog>
  );
}
