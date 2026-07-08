using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Commands.StartAdventure;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace FableFlow.Application.Tests.Adventures.Commands;

public class StartAdventureCommandHandlerTests
{
  private readonly IThemePolicyProvider _themeProvider = Substitute.For<IThemePolicyProvider>();
  private readonly IAdventureRepository _repository = Substitute.For<IAdventureRepository>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();
  private readonly IImageGenerationService _imageGeneration = Substitute.For<IImageGenerationService>();
  private readonly ISceneImageJobScheduler _imageJobScheduler = Substitute.For<ISceneImageJobScheduler>();

  private readonly StartAdventureCommandHandler _sut;

  public StartAdventureCommandHandlerTests()
  {
    _sut = new StartAdventureCommandHandler(
        _themeProvider,
        _repository,
        _promptBuilder,
        _storyGeneration,
        _imageGeneration,
        _imageJobScheduler);
  }

  [Fact]
  public async Task Handle_WithUnknownTheme_ThrowsNotFoundException()
  {
    _themeProvider.FindThemeAsync("unknown", Arg.Any<CancellationToken>())
        .Returns((ThemeDefinition?)null);

    var command = new StartAdventureCommand("unknown", 5);

    var act = () => _sut.Handle(command, CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  [Fact]
  public async Task Handle_WithKnownTheme_GeneratesInitialSceneAndSavesSession()
  {
    var theme = CreateTheme();
    _themeProvider.FindThemeAsync("pokemon", Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Initial, 1));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene(
            "Il était une fois...",
            [new GeneratedChoice("a", "Explorer", ChoiceOutcome.Neutral)],
            "Résumé initial",
            [],
            "Description générique de la scène"));

    _imageGeneration.IsEnabled.Returns(false);

    var result = await _sut.Handle(new StartAdventureCommand("pokemon", 5), CancellationToken.None);

    result.Status.Should().Be(nameof(SessionStatus.InProgress));
    result.CurrentScene.Should().NotBeNull();
    result.CurrentScene!.Text.Should().Be("Il était une fois...");
    result.CurrentScene.Choices.Should().ContainSingle(c => c.Id == "a");
    result.ImageGenerationEnabled.Should().BeFalse();

    await _repository.Received(1).SaveAsync(Arg.Any<AdventureSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithGeneratedStoryOutline_StoresItOnSession()
  {
    var theme = CreateTheme();
    _themeProvider.FindThemeAsync("pokemon", Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Initial, 1));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene(
            "Il était une fois...",
            [new GeneratedChoice("a", "Explorer", ChoiceOutcome.Neutral)],
            "Résumé initial",
            [],
            "Description générique de la scène",
            ["Introduction", "Complication", "Dénouement"]));

    _imageGeneration.IsEnabled.Returns(false);

    AdventureSession? savedSession = null;
    _repository.SaveAsync(Arg.Do<AdventureSession>(s => savedSession = s), Arg.Any<CancellationToken>())
        .Returns(Task.CompletedTask);

    await _sut.Handle(new StartAdventureCommand("pokemon", 5), CancellationToken.None);

    savedSession.Should().NotBeNull();
    savedSession!.StoryOutline.Should().Equal("Introduction", "Complication", "Dénouement");
  }

  [Fact]
  public async Task Handle_WhenCalled_ReturnsSceneWithoutWaitingForImageAndSchedulesItInBackground()
  {
    var theme = CreateTheme();
    _themeProvider.FindThemeAsync("pokemon", Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Initial, 1));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene(
            "Scène illustrée",
            [new GeneratedChoice("a", "Explorer", ChoiceOutcome.Neutral)],
            "Résumé",
            [],
            "Description générique de la scène"));

    _imageGeneration.IsEnabled.Returns(true);

    var result = await _sut.Handle(new StartAdventureCommand("pokemon", 5), CancellationToken.None);

    // Le texte et les choix sont retournés immédiatement ; l'image n'est jamais attendue ici,
    // elle est planifiée en arrière-plan (voir ISceneImageJobScheduler) et rattrapée par polling.
    result.CurrentScene!.ImageUrl.Should().BeNull();
    result.ImageGenerationEnabled.Should().BeTrue();

    _imageJobScheduler.Received(1).ScheduleForScene(
        Arg.Any<Guid>(),
        1,
        theme,
        "Description générique de la scène");
  }

  private static ThemeDefinition CreateTheme() => new(
      "pokemon",
      "Pokémon",
      AudienceTarget.Child,
      VocabularyLevel.Simple,
      "Univers de dresseurs",
      ["Aucune violence"],
      "style dessin animé");
}
