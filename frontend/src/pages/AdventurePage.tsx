import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import { isConflict, toApiError } from "../api/apiClient";
import type { AdventureDto } from "../api/types";
import { BookReader } from "../components/reader/BookReader";
import { EndingPanel, FrontierActions, WritingStatus } from "../components/reader/FrontierActions";
import { describeHero } from "../domain/hero";
import { frontierAction, resolveViewedChapterNumber, selectedChoiceLabel } from "../domain/reader";
import {
  describeSaveError,
  syncSavedBookReadingAids,
  useAdventure,
  useAutoSaveBook,
  useStoryAction,
  type StoryAction,
} from "../hooks/useAdventureReader";
import { useThemes } from "../hooks/useCatalog";
import { libraryKey, useSavedBook } from "../hooks/useLocalData";
import { getStoryReadingAids, setStoryReadingAids } from "../storage/readingPreferences";
import { protectedWords } from "../text/syllabify";

export function AdventurePage() {
  const { adventureId = "" } = useParams();
  return <LiveReader key={adventureId} adventureId={adventureId} />;
}

function LiveReader({ adventureId }: { adventureId: string }) {
  const adventureQuery = useAdventure(adventureId);
  const adventure = adventureQuery.data;

  if (adventure) {
    return <LiveStory adventure={adventure} />;
  }

  if (adventureQuery.isError) {
    return toApiError(adventureQuery.error).status === 404 ? (
      <StoryUnavailable adventureId={adventureId} />
    ) : (
      <Container sx={{ py: 4 }}>
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={() => void adventureQuery.refetch()}>
              Réessayer
            </Button>
          }
        >
          {toApiError(adventureQuery.error).message}
        </Alert>
      </Container>
    );
  }

  return (
    <Container sx={{ py: 6 }}>
      <WritingStatus label="Ouverture de l'histoire…" />
    </Container>
  );
}

function LiveStory({ adventure }: { adventure: AdventureDto }) {
  const queryClient = useQueryClient();
  const themesQuery = useThemes();
  const action = useStoryAction(adventure.adventureId);
  const [viewedNumber, setViewedNumber] = useState<number | null>(null);
  const [readingAids, setReadingAids] = useState(() => getStoryReadingAids(adventure.adventureId) ?? false);
  const headingRef = useRef<HTMLHeadingElement>(null);

  const themeName = themesQuery.data?.find((theme) => theme.id === adventure.themeId)?.displayName ?? null;
  const save = useAutoSaveBook(adventure, themeName, !themesQuery.isPending);
  const protectedNames = useMemo(
    () => protectedWords(adventure.hero.name, adventure.hero.companions),
    [adventure.hero.name, adventure.hero.companions],
  );

  const shownNumber = resolveViewedChapterNumber(adventure.chapters, viewedNumber);
  const chapter = adventure.chapters.find((c) => c.number === shownNumber) ?? adventure.chapters[0];
  const frontierNumber = adventure.chapters[adventure.chapters.length - 1]?.number ?? 1;
  const isFrontier = shownNumber === frontierNumber;
  const nextStep = frontierAction(adventure);

  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    const reduceMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
    window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
    headingRef.current?.focus({ preventScroll: true });
  }, [shownNumber]);

  const run = (storyAction: StoryAction) => {
    action.mutate(storyAction, {
      onSuccess: () => setViewedNumber(null),
      onError: (error) => {
        if (isConflict(error)) {
          setViewedNumber(null);
        }
      },
    });
  };

  const handleReadingAids = (value: boolean) => {
    setReadingAids(value);
    setStoryReadingAids(adventure.adventureId, value);
    if (adventure.status === "Completed") {
      void syncSavedBookReadingAids(adventure.adventureId, value).then(() =>
        queryClient.invalidateQueries({ queryKey: libraryKey }),
      );
    }
  };

  const actionError = action.isError ? toApiError(action.error) : null;
  const lastAction = action.variables;
  const subtitle = [describeHero(adventure.hero.name, adventure.hero.age), themeName].filter(Boolean).join(" · ");

  const actions = isFrontier ? (
    <Stack spacing={2}>
      {actionError && (
        <Alert
          severity={actionError.status === 409 ? "info" : "error"}
          action={
            actionError.status !== 409 && lastAction ? (
              <Button color="inherit" onClick={() => run(lastAction)}>
                Réessayer
              </Button>
            ) : undefined
          }
        >
          {actionError.status === 409
            ? `${actionError.message} Voici le dernier chapitre.`
            : actionError.message}
        </Alert>
      )}
      {nextStep === "finished" ? (
        <EndingPanel
          saveState={save.isError ? "error" : save.isSuccess ? "saved" : "saving"}
          saveError={save.isError ? describeSaveError(save.error) : null}
          onRetrySave={() => void save.refetch()}
          onReadAgain={() => setViewedNumber(1)}
        />
      ) : (
        <FrontierActions
          action={nextStep}
          chapter={chapter}
          heroName={adventure.hero.name}
          isPending={action.isPending}
          pendingChoiceId={action.isPending && lastAction?.type === "choose" ? lastAction.choiceId : null}
          readingAids={readingAids}
          largeText={adventure.readerMode === "ChildReader"}
          protectedNames={protectedNames}
          onAdvance={() => run({ type: "advance", fromChapterNumber: chapter.number })}
          onChoose={(choiceId) => run({ type: "choose", chapterNumber: chapter.number, choiceId })}
        />
      )}
    </Stack>
  ) : null;

  return (
    <BookReader
      title={adventure.title ?? `L'histoire de ${adventure.hero.name}`}
      subtitle={subtitle}
      chapter={{
        number: chapter.number,
        kind: chapter.kind,
        text: chapter.text,
        selectedChoiceLabel: selectedChoiceLabel(chapter),
        illustration: chapter.illustration,
      }}
      totalChapters={adventure.totalChapters}
      readableChapters={frontierNumber}
      onNavigate={setViewedNumber}
      readerMode={adventure.readerMode}
      readingAids={readingAids}
      onReadingAidsChange={handleReadingAids}
      protectedNames={protectedNames}
      showIllustrations={adventure.imageGenerationEnabled}
      isFrontier={isFrontier && adventure.status !== "Completed"}
      headingRef={headingRef}
      actions={actions}
    />
  );
}

function StoryUnavailable({ adventureId }: { adventureId: string }) {
  const savedBook = useSavedBook(adventureId);

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h1" sx={{ fontSize: "1.75rem", mb: 2 }}>
        Cette histoire n'est plus disponible
      </Typography>
      <Typography sx={{ mb: 3, maxWidth: "65ch" }}>
        Le serveur ne la connaît plus (il a peut-être redémarré). Les histoires terminées restent lisibles
        depuis la bibliothèque de cet appareil.
      </Typography>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
        {savedBook.data ? (
          <Button variant="contained" size="large" component={RouterLink} to={`/library/${adventureId}`}>
            Lire la version enregistrée
          </Button>
        ) : (
          <Button variant="contained" size="large" component={RouterLink} to="/create">
            Créer une nouvelle histoire
          </Button>
        )}
        <Button size="large" component={RouterLink} to="/library">
          Ma bibliothèque
        </Button>
      </Stack>
    </Container>
  );
}
