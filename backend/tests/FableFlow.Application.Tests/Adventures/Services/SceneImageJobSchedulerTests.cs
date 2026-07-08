using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Services;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace FableFlow.Application.Tests.Adventures.Services;

public class SceneImageJobSchedulerTests
{
  private readonly IBackgroundJobQueue _backgroundJobQueue = Substitute.For<IBackgroundJobQueue>();
  private readonly IAdventureRepository _repository = Substitute.For<IAdventureRepository>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IImageGenerationService _imageGeneration = Substitute.For<IImageGenerationService>();

  private readonly SceneImageJobScheduler _sut;

  public SceneImageJobSchedulerTests()
  {
    _sut = new SceneImageJobScheduler(_backgroundJobQueue, _repository, _promptBuilder, _imageGeneration);
  }

  [Fact]
  public void ScheduleForScene_WithImageGenerationDisabled_DoesNotQueueAnyWork()
  {
    _imageGeneration.IsEnabled.Returns(false);

    _sut.ScheduleForScene(Guid.NewGuid(), 1, CreateTheme(), "description");

    _backgroundJobQueue.DidNotReceiveWithAnyArgs().QueueBackgroundWorkItem(default!);
  }

  [Fact]
  public async Task ScheduleForScene_WithImageGenerationEnabled_AttachesImageToCorrectSceneAndSaves()
  {
    var theme = CreateTheme();
    var adventureId = Guid.NewGuid();
    var session = CreateSessionWithTwoScenes(adventureId);

    _imageGeneration.IsEnabled.Returns(true);
    _promptBuilder.BuildImagePrompt(theme, "description").Returns(new StoryImagePrompt("prompt", "style"));
    _imageGeneration.GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>())
        .Returns("https://images.example/scene-1.png");
    _repository.GetAsync(adventureId, Arg.Any<CancellationToken>()).Returns(session);

    Func<CancellationToken, Task>? queuedWork = null;
    _backgroundJobQueue.QueueBackgroundWorkItem(Arg.Do<Func<CancellationToken, Task>>(w => queuedWork = w));

    _sut.ScheduleForScene(adventureId, 1, theme, "description");

    queuedWork.Should().NotBeNull();
    await queuedWork!(CancellationToken.None);

    session.Scenes.Single(s => s.SceneNumber == 1).ImageUrl.Should().Be("https://images.example/scene-1.png");
    await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ScheduleForScene_WhenImageGenerationReturnsNull_DoesNotSave()
  {
    var theme = CreateTheme();
    var adventureId = Guid.NewGuid();

    _imageGeneration.IsEnabled.Returns(true);
    _promptBuilder.BuildImagePrompt(theme, "description").Returns(new StoryImagePrompt("prompt", "style"));
    _imageGeneration.GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>())
        .Returns((string?)null);

    Func<CancellationToken, Task>? queuedWork = null;
    _backgroundJobQueue.QueueBackgroundWorkItem(Arg.Do<Func<CancellationToken, Task>>(w => queuedWork = w));

    _sut.ScheduleForScene(adventureId, 1, theme, "description");
    await queuedWork!(CancellationToken.None);

    await _repository.DidNotReceiveWithAnyArgs().GetAsync(default, default);
    await _repository.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
  }

  [Fact]
  public async Task ScheduleForOutcome_WithImageGenerationEnabled_AttachesImageToOutcomeAndSaves()
  {
    var theme = CreateTheme();
    var adventureId = Guid.NewGuid();
    var session = CreateWonSession(adventureId);

    _imageGeneration.IsEnabled.Returns(true);
    _promptBuilder.BuildImagePrompt(theme, "description").Returns(new StoryImagePrompt("prompt", "style"));
    _imageGeneration.GenerateImageAsync(Arg.Any<StoryImagePrompt>(), Arg.Any<CancellationToken>())
        .Returns("https://images.example/outcome.png");
    _repository.GetAsync(adventureId, Arg.Any<CancellationToken>()).Returns(session);

    Func<CancellationToken, Task>? queuedWork = null;
    _backgroundJobQueue.QueueBackgroundWorkItem(Arg.Do<Func<CancellationToken, Task>>(w => queuedWork = w));

    _sut.ScheduleForOutcome(adventureId, theme, "description");
    await queuedWork!(CancellationToken.None);

    session.Outcome!.ImageUrl.Should().Be("https://images.example/outcome.png");
    await _repository.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
  }

  private static AdventureSession CreateSessionWithTwoScenes(Guid adventureId)
  {
    var session = AdventureSession.Start(adventureId, "pokemon", targetSceneCount: 5);
    session.AttachScene(CreateScene(1));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(2));
    return session;
  }

  private static AdventureSession CreateWonSession(Guid adventureId)
  {
    var session = AdventureSession.Start(adventureId, "pokemon", targetSceneCount: 3);
    session.AttachScene(CreateScene(1));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(2));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(3));
    session.RecordChoice("good");
    session.SetOutcome(AdventureOutcome.Won("Bravo !"));
    return session;
  }

  private static AdventureScene CreateScene(int sceneNumber) =>
      new(
          sceneNumber,
          $"Texte de la scène {sceneNumber}",
          [
              new SceneChoice("good", "Faire le bon choix", ChoiceOutcome.Good),
              new SceneChoice("bad", "Faire le mauvais choix", ChoiceOutcome.Bad)
          ]);

  private static ThemeDefinition CreateTheme() => new(
      "pokemon",
      "Pokémon",
      AudienceTarget.Child,
      VocabularyLevel.Simple,
      "Univers de dresseurs",
      ["Aucune violence"],
      "style dessin animé");
}
