using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Commands.MakeChoice;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace FableFlow.Application.Tests.Adventures.Commands;

public class MakeChoiceCommandHandlerTests
{
  private readonly IAdventureRepository _repository = Substitute.For<IAdventureRepository>();
  private readonly IThemePolicyProvider _themeProvider = Substitute.For<IThemePolicyProvider>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();
  private readonly IImageGenerationService _imageGeneration = Substitute.For<IImageGenerationService>();

  private readonly MakeChoiceCommandHandler _sut;

  public MakeChoiceCommandHandlerTests()
  {
    _imageGeneration.IsEnabled.Returns(false);

    _sut = new MakeChoiceCommandHandler(
        _repository,
        _themeProvider,
        _promptBuilder,
        _storyGeneration,
        _imageGeneration);
  }

  [Fact]
  public async Task Handle_WithUnknownAdventure_ThrowsNotFoundException()
  {
    _repository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns((AdventureSession?)null);

    var act = () => _sut.Handle(new MakeChoiceCommand(Guid.NewGuid(), "a"), CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  [Fact]
  public async Task Handle_WithGoodChoiceBeforeLastScene_GeneratesContinuationScene()
  {
    var theme = CreateTheme();
    var session = CreateSessionAtFirstScene(targetSceneCount: 3);

    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _themeProvider.FindThemeAsync(theme.Id, Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Continuation, 2));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene(
            "Suite de l'histoire",
            [new GeneratedChoice("a", "Continuer", ChoiceOutcome.Neutral)],
            "Résumé mis à jour",
            [],
            "Description générique de la scène"));

    var result = await _sut.Handle(new MakeChoiceCommand(session.Id, "good"), CancellationToken.None);

    result.Status.Should().Be(nameof(SessionStatus.InProgress));
    result.CurrentScene!.SceneNumber.Should().Be(2);
    result.CurrentScene.Text.Should().Be("Suite de l'histoire");

    await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReachingMaxBadChoices_GeneratesEndingAndSetsLostOutcome()
  {
    var theme = CreateTheme();
    var session = CreateSessionAtFirstScene(targetSceneCount: 10, maxBadChoices: 1);

    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _themeProvider.FindThemeAsync(theme.Id, Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Ending, 1));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene("Fin tragique", [], "Résumé final", [], "Description générique de la scène"));

    var result = await _sut.Handle(new MakeChoiceCommand(session.Id, "bad"), CancellationToken.None);

    result.Status.Should().Be(nameof(SessionStatus.Lost));
    result.CurrentScene.Should().BeNull();
    result.OutcomeMessage.Should().Be("Fin tragique");
  }

  [Fact]
  public async Task Handle_ReachingMaxBadChoices_WithImageGenerationEnabled_AttachesOutcomeImage()
  {
    var theme = CreateTheme();
    var session = CreateSessionAtFirstScene(targetSceneCount: 10, maxBadChoices: 1);

    _repository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
    _themeProvider.FindThemeAsync(theme.Id, Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildScenePrompt(Arg.Any<SceneGenerationRequest>())
        .Returns(new StoryPrompt("system", "user", "v1", SceneKind.Ending, 1));

    _storyGeneration.GenerateSceneAsync(Arg.Any<StoryPrompt>(), Arg.Any<CancellationToken>())
        .Returns(new GeneratedScene("Fin tragique", [], "Résumé final", [], "Description générique de la scène finale"));

    _imageGeneration.IsEnabled.Returns(true);
    _promptBuilder.BuildImagePrompt(Arg.Any<SceneGenerationRequest>(), Arg.Any<string>())
        .Returns(new StoryImagePrompt("prompt", "style"));
    _imageGeneration.GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>())
        .Returns("data:image/jpeg;base64,abc123");

    var result = await _sut.Handle(new MakeChoiceCommand(session.Id, "bad"), CancellationToken.None);

    result.Status.Should().Be(nameof(SessionStatus.Lost));
    result.OutcomeImageUrl.Should().Be("data:image/jpeg;base64,abc123");
  }

  private static AdventureSession CreateSessionAtFirstScene(int targetSceneCount, int maxBadChoices = 3)
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount, maxBadChoices);
    session.AttachScene(new AdventureScene(
        1,
        "Scène initiale",
        [
            new SceneChoice("good", "Faire le bon choix", ChoiceOutcome.Good),
                new SceneChoice("bad", "Faire le mauvais choix", ChoiceOutcome.Bad)
        ]));
    return session;
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
