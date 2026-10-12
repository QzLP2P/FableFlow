using FableFlow.Application.Abstractions;
using FableFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FableFlow.Application.Adventures.Services;

/// <summary>
/// Implémentation par défaut de <see cref="IChapterImageJobScheduler"/> : construit le prompt
/// d'illustration, génère l'image via <see cref="IImageGenerationService"/>, puis recharge la
/// session (elle a pu évoluer entre-temps) pour marquer l'illustration prête ou indisponible.
/// </summary>
public sealed class ChapterImageJobScheduler : IChapterImageJobScheduler
{
  private readonly IBackgroundJobQueue _backgroundJobQueue;
  private readonly IAdventureRepository _repository;
  private readonly IPromptBuilder _promptBuilder;
  private readonly IImageGenerationService _imageGeneration;
  private readonly ILogger<ChapterImageJobScheduler> _logger;

  public ChapterImageJobScheduler(
      IBackgroundJobQueue backgroundJobQueue,
      IAdventureRepository repository,
      IPromptBuilder promptBuilder,
      IImageGenerationService imageGeneration,
      ILogger<ChapterImageJobScheduler> logger)
  {
    _backgroundJobQueue = backgroundJobQueue;
    _repository = repository;
    _promptBuilder = promptBuilder;
    _imageGeneration = imageGeneration;
    _logger = logger;
  }

  public void ScheduleForChapter(
      AdventureSession session,
      int chapterNumber,
      ThemeDefinition theme,
      string? genericSceneDescription)
  {
    ArgumentNullException.ThrowIfNull(session);

    if (!_imageGeneration.IsEnabled)
    {
      return;
    }

    if (string.IsNullOrWhiteSpace(genericSceneDescription))
    {
      session.MarkIllustrationFailed(chapterNumber);
      return;
    }

    session.MarkIllustrationPending(chapterNumber);

    // Seules des valeurs immuables sont capturées : la session vivante est relue après génération.
    var adventureId = session.Id;
    var hero = session.Hero;

    _backgroundJobQueue.QueueBackgroundWorkItem(async cancellationToken =>
    {
      var imageUrl = await TryGenerateImageAsync(theme, hero, genericSceneDescription, chapterNumber, cancellationToken);

      var current = await _repository.GetAsync(adventureId, cancellationToken);
      if (current is null)
      {
        return;
      }

      if (imageUrl is null)
      {
        current.MarkIllustrationFailed(chapterNumber);
      }
      else
      {
        current.AttachIllustration(chapterNumber, imageUrl);
      }

      await _repository.SaveAsync(current, cancellationToken);
    });
  }

  private async Task<string?> TryGenerateImageAsync(
      ThemeDefinition theme,
      HeroProfile hero,
      string genericSceneDescription,
      int chapterNumber,
      CancellationToken cancellationToken)
  {
    try
    {
      var imagePrompt = _promptBuilder.BuildImagePrompt(theme, hero, genericSceneDescription);
      return await _imageGeneration.GenerateImageAsync(imagePrompt, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
    {
      _logger.LogWarning(ex, "Illustration indisponible pour le chapitre {ChapterNumber}", chapterNumber);
      return null;
    }
  }
}
