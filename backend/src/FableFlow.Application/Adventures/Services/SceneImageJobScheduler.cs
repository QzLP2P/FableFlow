using FableFlow.Application.Abstractions;
using FableFlow.Domain.Entities;

namespace FableFlow.Application.Adventures.Services;

/// <summary>
/// Implémentation par défaut de <see cref="ISceneImageJobScheduler"/> : construit le prompt
/// d'illustration, génère l'image via <see cref="IImageGenerationService"/>, puis recharge la
/// session depuis <see cref="IAdventureRepository"/> (elle a pu évoluer entre-temps) pour y attacher
/// l'image et la sauvegarder. Le tout est planifié via <see cref="IBackgroundJobQueue"/> afin de ne
/// jamais retarder la réponse HTTP contenant le texte et les choix.
/// </summary>
public sealed class SceneImageJobScheduler : ISceneImageJobScheduler
{
  private readonly IBackgroundJobQueue _backgroundJobQueue;
  private readonly IAdventureRepository _repository;
  private readonly IPromptBuilder _promptBuilder;
  private readonly IImageGenerationService _imageGeneration;

  public SceneImageJobScheduler(
      IBackgroundJobQueue backgroundJobQueue,
      IAdventureRepository repository,
      IPromptBuilder promptBuilder,
      IImageGenerationService imageGeneration)
  {
    _backgroundJobQueue = backgroundJobQueue;
    _repository = repository;
    _promptBuilder = promptBuilder;
    _imageGeneration = imageGeneration;
  }

  public void ScheduleForScene(Guid adventureId, int sceneNumber, ThemeDefinition theme, string genericSceneDescription)
  {
    if (!_imageGeneration.IsEnabled)
    {
      return;
    }

    _backgroundJobQueue.QueueBackgroundWorkItem(async cancellationToken =>
    {
      var imageUrl = await GenerateImageAsync(theme, genericSceneDescription, cancellationToken);
      if (imageUrl is null)
      {
        return;
      }

      var session = await _repository.GetAsync(adventureId, cancellationToken);
      if (session is null)
      {
        return;
      }

      session.AttachImageToScene(sceneNumber, imageUrl);
      await _repository.SaveAsync(session, cancellationToken);
    });
  }

  public void ScheduleForOutcome(Guid adventureId, ThemeDefinition theme, string genericSceneDescription)
  {
    if (!_imageGeneration.IsEnabled)
    {
      return;
    }

    _backgroundJobQueue.QueueBackgroundWorkItem(async cancellationToken =>
    {
      var imageUrl = await GenerateImageAsync(theme, genericSceneDescription, cancellationToken);
      if (imageUrl is null)
      {
        return;
      }

      var session = await _repository.GetAsync(adventureId, cancellationToken);
      if (session is null)
      {
        return;
      }

      session.AttachImageToOutcome(imageUrl);
      await _repository.SaveAsync(session, cancellationToken);
    });
  }

  private async Task<string?> GenerateImageAsync(
      ThemeDefinition theme,
      string genericSceneDescription,
      CancellationToken cancellationToken)
  {
    var imagePrompt = _promptBuilder.BuildImagePrompt(theme, genericSceneDescription);
    return await _imageGeneration.GenerateImageAsync(imagePrompt, cancellationToken);
  }
}
