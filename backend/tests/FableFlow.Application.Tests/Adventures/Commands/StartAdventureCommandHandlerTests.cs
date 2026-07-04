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

  private readonly StartAdventureCommandHandler _sut;

  public StartAdventureCommandHandlerTests()
  {
    _sut = new StartAdventureCommandHandler(
        _themeProvider,
        _repository,
        _promptBuilder,
        _storyGeneration,
        _imageGeneration);
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
            []));

    _imageGeneration.IsEnabled.Returns(false);

    var result = await _sut.Handle(new StartAdventureCommand("pokemon", 5), CancellationToken.None);

    result.Status.Should().Be(nameof(SessionStatus.InProgress));
    result.CurrentScene.Should().NotBeNull();
    result.CurrentScene!.Text.Should().Be("Il était une fois...");
    result.CurrentScene.Choices.Should().ContainSingle(c => c.Id == "a");

    await _repository.Received(1).SaveAsync(Arg.Any<AdventureSession>(), Arg.Any<CancellationToken>());
    await _imageGeneration.DidNotReceive().GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithImageGenerationEnabled_AttachesGeneratedImage()
  {
    var theme = CreateTheme();
    _themeProvider.FindThemeAsync("pokemon", Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Initial, 1));
    _promptBuilder.BuildImagePrompt(Arg.Any<SceneGenerationRequest>(), Arg.Any<string>())
        .Returns(new StoryImagePrompt("prompt", "style"));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene(
            "Scène illustrée",
            [new GeneratedChoice("a", "Explorer", ChoiceOutcome.Neutral)],
            "Résumé",
            []));

    _imageGeneration.IsEnabled.Returns(true);
    _imageGeneration.GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>())
        .Returns("https://images.example/scene.png");

    var result = await _sut.Handle(new StartAdventureCommand("pokemon", 5), CancellationToken.None);

    result.CurrentScene!.ImageUrl.Should().Be("https://images.example/scene.png");
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
