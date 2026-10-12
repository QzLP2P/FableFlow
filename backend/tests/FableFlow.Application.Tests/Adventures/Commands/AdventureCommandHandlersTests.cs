using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Commands.AdvanceChapter;
using FableFlow.Application.Adventures.Commands.MakeChoice;
using FableFlow.Application.Adventures.Commands.StartAdventure;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Tests.TestSupport;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FableFlow.Application.Tests.Adventures.Commands;

/// <summary>Handlers testés avec le vrai <see cref="ChapterWriter"/> et des ports simulés.</summary>
public class AdventureCommandHandlersTests
{
  private readonly IAdventureRepository _repository = Substitute.For<IAdventureRepository>();
  private readonly IThemePolicyProvider _themeProvider = Substitute.For<IThemePolicyProvider>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();
  private readonly IImageGenerationService _imageGeneration = Substitute.For<IImageGenerationService>();
  private readonly IChapterImageJobScheduler _imageScheduler = Substitute.For<IChapterImageJobScheduler>();
  private readonly ChapterWriter _writer;

  public AdventureCommandHandlersTests()
  {
    _themeProvider.FindThemeAsync("dinosaur", Arg.Any<CancellationToken>()).Returns(TestData.Theme());
    _promptBuilder.BuildChapterPrompt(default!).ReturnsForAnyArgs(TestData.Prompt());
    _writer = new ChapterWriter(_promptBuilder, _storyGeneration, _imageScheduler, NullLogger<ChapterWriter>.Instance);
  }

  [Fact]
  public async Task Start_WritesOpeningFromSnapshotAndSaves()
  {
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Narrative());
    var handler = new StartAdventureCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var result = await handler.Handle(
        new StartAdventureCommand("dinosaur", StoryLength.Longer, ReaderMode.ChildReader, TestData.HeroInput(), "Un œuf mystérieux", "L'œuf"),
        CancellationToken.None);

    result.Status.Should().Be(SessionStatus.InProgress);
    result.TotalChapters.Should().Be(10);
    result.DecisionChapters.Should().Equal(3, 5, 8);
    result.ReaderMode.Should().Be(ReaderMode.ChildReader);
    result.Hero.Name.Should().Be("Léa");
    result.Hero.Companions.Should().Equal("Max, son chien");
    result.Title.Should().Be("L'œuf");
    result.CurrentChapterNumber.Should().Be(1);
    result.Chapters.Should().ContainSingle().Which.Choices.Should().BeEmpty();
    _promptBuilder.Received(1).BuildChapterPrompt(Arg.Is<ChapterGenerationRequest>(r =>
        r.Session.Hero.Age == 6 && r.Session.Premise == "Un œuf mystérieux" && r.Transition.NextChapterNumber == 1));
    await _repository.Received(1).SaveAsync(Arg.Any<AdventureSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Start_WithUnknownTheme_ThrowsNotFound()
  {
    var handler = new StartAdventureCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(
        new StartAdventureCommand("unknown", StoryLength.Short, ReaderMode.AdultReadAloud, TestData.HeroInput(), "Idée"),
        CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  [Fact]
  public async Task Start_WhenGenerationFails_SavesNothing()
  {
    _storyGeneration.GenerateChapterAsync(default!, default).ThrowsAsyncForAnyArgs(new HttpRequestException());
    var handler = new StartAdventureCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(
        new StartAdventureCommand("dinosaur", StoryLength.Short, ReaderMode.AdultReadAloud, TestData.HeroInput(), "Idée"),
        CancellationToken.None);

    await act.Should().ThrowAsync<StoryGenerationException>();
    await _repository.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
  }

  [Fact]
  public async Task Advance_FromNarrativeChapter_WritesNextChapter()
  {
    var session = TestData.Opened();
    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Decision("Entrer", "Attendre"));
    var handler = new AdvanceChapterCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var result = await handler.Handle(new AdvanceChapterCommand(session.Id, 1), CancellationToken.None);

    result.CurrentChapterNumber.Should().Be(2);
    result.Chapters[1].Kind.Should().Be(ChapterKind.Decision);
    result.Chapters[1].Choices.Should().HaveCount(2);
  }

  [Fact]
  public async Task Advance_OnDecisionChapter_IsRejectedWithoutGenerating()
  {
    var session = TestData.AtFirstDecision();
    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    var handler = new AdvanceChapterCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(new AdvanceChapterCommand(session.Id, 2), CancellationToken.None);

    await act.Should().ThrowAsync<StoryConflictException>();
    await _storyGeneration.DidNotReceiveWithAnyArgs().GenerateChapterAsync(default!, default);
  }

  [Fact]
  public async Task Advance_UnknownAdventure_ThrowsNotFound()
  {
    var handler = new AdvanceChapterCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(new AdvanceChapterCommand(Guid.NewGuid(), 1), CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  [Fact]
  public async Task Choice_RecordsSelectedActionAndWritesFollowingChapter()
  {
    var session = TestData.AtFirstDecision();
    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Narrative("La grotte brille."));
    var handler = new MakeChoiceCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var result = await handler.Handle(new MakeChoiceCommand(session.Id, 2, "b"), CancellationToken.None);

    result.Chapters[1].SelectedChoiceId.Should().Be("b");
    result.Chapters[2].Text.Should().Be("La grotte brille.");
    _promptBuilder.Received(1).BuildChapterPrompt(Arg.Is<ChapterGenerationRequest>(r =>
        r.Transition.SelectedChoice!.Label == "Attendre"));
  }

  [Fact]
  public async Task Choice_WhenGenerationFails_KeepsChoiceUnrecordedAndAllowsRetry()
  {
    var session = TestData.AtFirstDecision();
    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _storyGeneration.GenerateChapterAsync(default!, default).ThrowsAsyncForAnyArgs(new HttpRequestException());
    var handler = new MakeChoiceCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(new MakeChoiceCommand(session.Id, 2, "a"), CancellationToken.None);
    await act.Should().ThrowAsync<StoryGenerationException>();

    session.Chapters[1].SelectedChoiceId.Should().BeNull();
    session.IsGenerating.Should().BeFalse();

    _storyGeneration.GenerateChapterAsync(default!, default).ReturnsForAnyArgs(TestData.Narrative());
    var result = await handler.Handle(new MakeChoiceCommand(session.Id, 2, "b"), CancellationToken.None);
    result.Chapters[1].SelectedChoiceId.Should().Be("b");
  }

  [Fact]
  public async Task Choice_UnknownChoice_ThrowsDomainException()
  {
    var session = TestData.AtFirstDecision();
    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    var handler = new MakeChoiceCommandHandler(_repository, _themeProvider, _writer, _imageGeneration);

    var act = () => handler.Handle(new MakeChoiceCommand(session.Id, 2, "z"), CancellationToken.None);

    await act.Should().ThrowAsync<DomainException>();
  }
}
