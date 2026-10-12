using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FluentAssertions;

namespace FableFlow.Domain.Tests.Entities;

public class AdventureSessionTests
{
  [Fact]
  public void Start_WithValidArguments_CreatesInProgressSessionWithSnapshot()
  {
    var hero = HeroProfile.Create("Léa", 5, ["Max, son chien"], interests: "les étoiles");

    var session = AdventureSession.Start(
        Guid.NewGuid(), "dinosaur", hero, ReaderMode.ChildReader, StoryLength.Short, "  Un œuf mystérieux  ", "L'œuf");

    session.Status.Should().Be(SessionStatus.InProgress);
    session.Hero.Should().BeSameAs(hero);
    session.ReaderMode.Should().Be(ReaderMode.ChildReader);
    session.Length.Should().Be(StoryLength.Short);
    session.TotalChapters.Should().Be(6);
    session.Premise.Should().Be("Un œuf mystérieux");
    session.Title.Should().Be("L'œuf");
    session.CurrentChapterNumber.Should().Be(0);
    session.CurrentChapter.Should().BeNull();
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Start_WithoutPremise_Throws(string? premise)
  {
    var act = () => AdventureSession.Start(
        Guid.NewGuid(), "dinosaur", Hero(), ReaderMode.AdultReadAloud, StoryLength.Short, premise!);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void Start_WithTooLongPremise_Throws()
  {
    var act = () => AdventureSession.Start(
        Guid.NewGuid(), "dinosaur", Hero(), ReaderMode.AdultReadAloud, StoryLength.Short,
        new string('a', AdventureSession.MaxPremiseLength + 1));

    act.Should().Throw<DomainException>();
  }

  [Theory]
  [InlineData(StoryLength.Short, 6)]
  [InlineData(StoryLength.Longer, 10)]
  public void FullStory_FollowsScheduleAndCompletesOnEndingChapter(StoryLength length, int expectedChapters)
  {
    var session = NewSession(length);

    PlayToEnd(session);

    session.Chapters.Should().HaveCount(expectedChapters);
    session.Status.Should().Be(SessionStatus.Completed);
    session.Chapters[0].Kind.Should().Be(ChapterKind.Narrative);
    session.Chapters[^1].Kind.Should().Be(ChapterKind.Ending);
    session.Chapters[0].Choices.Should().BeEmpty();
    session.Chapters[^1].Choices.Should().BeEmpty();
    session.Chapters.Where(c => c.Kind == ChapterKind.Decision)
        .Should().OnlyContain(c => c.SelectedChoiceId != null);
  }

  [Fact]
  public void BeginOpening_TwiceWhilePending_IsRejected()
  {
    var session = NewSession();
    session.BeginOpening();

    var act = () => session.BeginOpening();

    act.Should().Throw<StoryConflictException>();
  }

  [Fact]
  public void BeginOpening_AfterStoryStarted_IsRejected()
  {
    var session = NewSession();
    Open(session);

    var act = () => session.BeginOpening();

    act.Should().Throw<StoryConflictException>();
  }

  [Fact]
  public void BeginAdvance_OnNarrativeFrontier_ReservesNextChapterOfScheduledKind()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);

    var transition = session.BeginAdvance(1);

    transition.FromChapterNumber.Should().Be(1);
    transition.NextChapterNumber.Should().Be(2);
    transition.NextKind.Should().Be(ChapterKind.Decision);
    transition.SelectedChoice.Should().BeNull();
    session.IsGenerating.Should().BeTrue();
  }

  [Fact]
  public void BeginAdvance_OnDecisionChapter_IsRejectedAsWrongAction()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session); // chapitre 2 = moment de choix

    var act = () => session.BeginAdvance(2);

    act.Should().Throw<StoryConflictException>().WithMessage("*choix*");
  }

  [Fact]
  public void BeginChoice_OnNarrativeChapter_IsRejectedAsWrongAction()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);

    var act = () => session.BeginChoice(1, "a");

    act.Should().Throw<StoryConflictException>();
  }

  [Fact]
  public void BeginAdvance_FromStaleChapter_IsRejectedAsDuplicate()
  {
    var session = NewSession(StoryLength.Longer);
    Open(session);
    Advance(session); // frontière = 2

    var act = () => session.BeginAdvance(1);

    act.Should().Throw<StoryConflictException>().WithMessage("*chapitre courant*");
  }

  [Fact]
  public void BeginAdvance_WhileGenerationPending_IsRejected()
  {
    var session = NewSession();
    Open(session);
    session.BeginAdvance(1);

    var act = () => session.BeginAdvance(1);

    act.Should().Throw<StoryConflictException>().WithMessage("*en cours*");
  }

  [Fact]
  public void BeginChoice_WithUnknownChoice_Throws()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session);

    var act = () => session.BeginChoice(2, "zzz");

    act.Should().Throw<DomainException>();
    session.IsGenerating.Should().BeFalse();
  }

  [Fact]
  public void CompleteTransition_AfterChoice_RecordsChoiceOnlyWhenNextChapterIsAttached()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session);

    var transition = session.BeginChoice(2, "B");
    session.Chapters[1].SelectedChoiceId.Should().BeNull();

    session.CompleteTransition(transition, ChapterFor(transition));

    session.Chapters[1].SelectedChoiceId.Should().Be("b");
    session.Chapters[1].SelectedChoice!.Label.Should().Be("Suivre le papillon");
    session.CurrentChapterNumber.Should().Be(3);
  }

  [Fact]
  public void AbandonTransition_AfterFailure_KeepsStateAndAllowsRetry()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session);
    var failed = session.BeginChoice(2, "a");

    session.AbandonTransition(failed);

    session.CurrentChapterNumber.Should().Be(2);
    session.Chapters[1].SelectedChoiceId.Should().BeNull();
    session.IsGenerating.Should().BeFalse();

    var retry = session.BeginChoice(2, "b");
    session.CompleteTransition(retry, ChapterFor(retry));
    session.Chapters[1].SelectedChoiceId.Should().Be("b");
  }

  [Fact]
  public void CompleteTransition_WithAbandonedToken_IsRejected()
  {
    var session = NewSession();
    var transition = session.BeginOpening();
    session.AbandonTransition(transition);

    var act = () => session.CompleteTransition(transition, ChapterFor(transition));

    act.Should().Throw<StoryConflictException>();
    session.Chapters.Should().BeEmpty();
  }

  [Fact]
  public void CompleteTransition_WithChapterOfWrongKind_IsRejectedAndKeepsReservation()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    var transition = session.BeginAdvance(1); // attend un moment de choix

    var act = () => session.CompleteTransition(
        transition, new AdventureChapter(2, ChapterKind.Narrative, "Texte", []));

    act.Should().Throw<DomainException>();
    session.CurrentChapterNumber.Should().Be(1);
  }

  [Fact]
  public void BeginAdvance_AfterCompletion_IsRejected()
  {
    var session = NewSession(StoryLength.Short);
    PlayToEnd(session);

    var act = () => session.BeginAdvance(6);

    act.Should().Throw<StoryConflictException>().WithMessage("*terminée*");
  }

  [Fact]
  public void BeginChoice_OnAlreadyChosenChapter_IsRejectedAsDuplicate()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session);
    Choose(session, 2, "a");

    var act = () => session.BeginChoice(2, "a");

    act.Should().Throw<StoryConflictException>();
  }

  [Fact]
  public void Illustrations_TrackPendingReadyAndFailedStates()
  {
    var session = NewSession(StoryLength.Short);
    Open(session);
    Advance(session);

    session.MarkIllustrationPending(1);
    session.MarkIllustrationPending(2);
    session.AttachIllustration(1, "https://img/1.png");
    session.MarkIllustrationFailed(2);
    session.MarkIllustrationFailed(1);

    session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Ready);
    session.Chapters[0].ImageUrl.Should().Be("https://img/1.png");
    session.Chapters[1].IllustrationStatus.Should().Be(IllustrationStatus.Failed);
    session.Chapters[1].ImageUrl.Should().BeNull();
  }

  [Fact]
  public void SetTitleIfMissing_DoesNotOverrideChosenTitle()
  {
    var session = AdventureSession.Start(
        Guid.NewGuid(), "vet", Hero(), ReaderMode.AdultReadAloud, StoryLength.Short, "Idée", "Titre choisi");

    session.SetTitleIfMissing("Autre titre");

    session.Title.Should().Be("Titre choisi");
  }

  [Fact]
  public void SetTitleIfMissing_FillsTitleForCustomIdea()
  {
    var session = NewSession();

    session.SetTitleIfMissing("  La forêt qui chante ");

    session.Title.Should().Be("La forêt qui chante");
  }

  [Fact]
  public void UpdateMemory_KeepsPreviousValuesWhenEmptyAndBoundsKeyFacts()
  {
    var session = NewSession();
    session.UpdateMemory("Résumé", Enumerable.Range(1, 20).Select(i => $"fait {i}"));

    session.UpdateMemory("  ", []);

    session.RunningSummary.Should().Be("Résumé");
    session.KeyFacts.Should().HaveCount(AdventureSession.MaxKeyFacts);
  }

  [Fact]
  public void SetStoryOutline_IsFixedOnce()
  {
    var session = NewSession();

    session.SetStoryOutline(["Début", "Fin"]);
    session.SetStoryOutline(["Autre"]);

    session.StoryOutline.Should().Equal("Début", "Fin");
  }

  private static HeroProfile Hero() => HeroProfile.Create("Léa", 6);

  private static AdventureSession NewSession(StoryLength length = StoryLength.Short) =>
      AdventureSession.Start(Guid.NewGuid(), "dinosaur", Hero(), ReaderMode.AdultReadAloud, length, "Un œuf mystérieux");

  private static AdventureChapter ChapterFor(ChapterTransition transition) =>
      new(
          transition.NextChapterNumber,
          transition.NextKind,
          $"Chapitre {transition.NextChapterNumber}",
          transition.NextKind == ChapterKind.Decision
              ? [new ChapterChoice("a", "Entrer dans la grotte"), new ChapterChoice("b", "Suivre le papillon")]
              : []);

  private static void Open(AdventureSession session)
  {
    var transition = session.BeginOpening();
    session.CompleteTransition(transition, ChapterFor(transition));
  }

  private static void Advance(AdventureSession session)
  {
    var transition = session.BeginAdvance(session.CurrentChapterNumber);
    session.CompleteTransition(transition, ChapterFor(transition));
  }

  private static void Choose(AdventureSession session, int chapterNumber, string choiceId)
  {
    var transition = session.BeginChoice(chapterNumber, choiceId);
    session.CompleteTransition(transition, ChapterFor(transition));
  }

  private static void PlayToEnd(AdventureSession session)
  {
    Open(session);
    while (!session.IsCompleted)
    {
      var current = session.CurrentChapter!;
      if (current.Kind == ChapterKind.Decision)
      {
        Choose(session, current.Number, "a");
      }
      else
      {
        Advance(session);
      }
    }
  }
}
