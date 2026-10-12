using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Tests.TestSupport;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FableFlow.Application.Tests.Adventures.Services;

public class ChapterWriterTests
{
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();
  private readonly IChapterImageJobScheduler _imageScheduler = Substitute.For<IChapterImageJobScheduler>();
  private readonly ChapterWriter _writer;

  public ChapterWriterTests()
  {
    _promptBuilder.BuildChapterPrompt(Arg.Any<ChapterGenerationRequest>()).Returns(TestData.Prompt());
    _writer = new ChapterWriter(_promptBuilder, _storyGeneration, _imageScheduler, NullLogger<ChapterWriter>.Instance);
  }

  [Fact]
  public async Task Opening_AttachesNarrativeChapterAndFixesOutlineAndTitle()
  {
    var session = TestData.NewSession();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Narrative());

    await _writer.WriteAsync(session, TestData.Theme(), session.BeginOpening(), CancellationToken.None);

    session.Chapters.Should().ContainSingle().Which.Kind.Should().Be(ChapterKind.Narrative);
    session.StoryOutline.Should().Equal("Début", "Milieu", "Fin");
    session.Title.Should().Be("Le grand œuf");
    session.RunningSummary.Should().Be("Résumé");
    session.IsGenerating.Should().BeFalse();
    _imageScheduler.Received(1).ScheduleForChapter(session, 1, Arg.Any<ThemeDefinition>(), "Une vallée verdoyante au lever du soleil");
  }

  [Fact]
  public async Task PromptReceivesReservedTransition()
  {
    var session = TestData.Opened();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Decision("Entrer", "Attendre"));

    var transition = session.BeginAdvance(1);
    await _writer.WriteAsync(session, TestData.Theme(), transition, CancellationToken.None);

    _promptBuilder.Received(1).BuildChapterPrompt(Arg.Is<ChapterGenerationRequest>(r =>
        r.Session == session && r.Transition == transition && r.Transition.NextKind == ChapterKind.Decision));
  }

  [Fact]
  public async Task Decision_NormalizesChoicesToAtMostThreeDistinctActions()
  {
    var session = TestData.Opened();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(
        TestData.Decision(" Entrer dans la grotte ", "entrer dans la grotte", "Suivre le papillon", "Appeler Max", "Dormir"));

    await _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    var chapter = session.CurrentChapter!;
    chapter.Choices.Select(c => c.Id).Should().Equal("a", "b", "c");
    chapter.Choices.Select(c => c.Label).Should().Equal("Entrer dans la grotte", "Suivre le papillon", "Appeler Max");
  }

  [Fact]
  public async Task Narrative_IgnoresUnexpectedChoices()
  {
    var session = TestData.Opened(StoryLength.Longer); // chapitre 2 = récit
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Decision("Entrer", "Attendre"));

    await _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    session.CurrentChapter!.Kind.Should().Be(ChapterKind.Narrative);
    session.CurrentChapter.Choices.Should().BeEmpty();
  }

  [Fact]
  public async Task InvalidShape_IsRetriedOnceThenSucceeds()
  {
    var session = TestData.Opened();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(
        TestData.Decision("Une seule action"),
        TestData.Decision("Entrer", "Attendre"));

    await _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    await _storyGeneration.ReceivedWithAnyArgs(2).GenerateChapterAsync(default!, default);
    session.CurrentChapterNumber.Should().Be(2);
  }

  [Fact]
  public async Task PersistentlyInvalidShape_FailsCleanlyAndReleasesReservation()
  {
    var session = TestData.Opened();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Decision("Une seule action"));

    var act = () => _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    await act.Should().ThrowAsync<StoryGenerationException>();
    await _storyGeneration.ReceivedWithAnyArgs(ChapterWriter.MaxAttempts).GenerateChapterAsync(default!, default);
    session.CurrentChapterNumber.Should().Be(1);
    session.IsGenerating.Should().BeFalse();
    _imageScheduler.DidNotReceiveWithAnyArgs().ScheduleForChapter(default!, default, default!, default);
  }

  [Fact]
  public async Task ProviderFailure_IsWrappedWithSanitizedMessage()
  {
    var session = TestData.Opened();
    _storyGeneration.GenerateChapterAsync(default!, default)
        .ThrowsAsyncForAnyArgs(new HttpRequestException("secret endpoint https://internal"));

    var act = () => _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    var error = await act.Should().ThrowAsync<StoryGenerationException>();
    error.Which.Message.Should().NotContain("internal");
    session.IsGenerating.Should().BeFalse();
  }

  [Fact]
  public async Task Cancellation_PropagatesAndReleasesReservation()
  {
    var session = TestData.Opened();
    using var cts = new CancellationTokenSource();
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs<GeneratedChapter>(_ =>
    {
      cts.Cancel();
      throw new OperationCanceledException(cts.Token);
    });

    var act = () => _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), cts.Token);

    await act.Should().ThrowAsync<OperationCanceledException>();
    session.IsGenerating.Should().BeFalse();
    await _storyGeneration.ReceivedWithAnyArgs(1).GenerateChapterAsync(default!, default);
  }

  [Fact]
  public async Task Continuation_DoesNotOverrideOutlineOrTitle()
  {
    var session = TestData.NewSession(title: "Titre choisi");
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Narrative());
    await _writer.WriteAsync(session, TestData.Theme(), session.BeginOpening(), CancellationToken.None);

    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(
        TestData.Decision("Entrer", "Attendre") with { StoryOutline = ["Autre"], Title = "Autre titre" });
    await _writer.WriteAsync(session, TestData.Theme(), session.BeginAdvance(1), CancellationToken.None);

    session.Title.Should().Be("Titre choisi");
    session.StoryOutline.Should().Equal("Début", "Milieu", "Fin");
  }
}
