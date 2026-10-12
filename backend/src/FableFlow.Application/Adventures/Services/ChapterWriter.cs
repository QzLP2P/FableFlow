using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FableFlow.Application.Adventures.Services;

/// <summary>
/// Implémentation de <see cref="IChapterWriter"/>. La forme du chapitre généré est contrôlée selon
/// sa nature attendue (aucun choix hors moment de choix, 2 à 3 actions distinctes sinon) ; une
/// réponse inexploitable est redemandée une fois avant d'échouer proprement.
/// </summary>
public sealed class ChapterWriter : IChapterWriter
{
  public const int MaxAttempts = 2;

  private const string _generationFailedMessage =
      "La suite de l'histoire n'a pas pu être écrite pour le moment. Réessayez dans un instant.";

  private static readonly string[] _choiceIds = ["a", "b", "c"];

  private readonly IPromptBuilder _promptBuilder;
  private readonly IStoryGenerationService _storyGeneration;
  private readonly IChapterImageJobScheduler _imageJobScheduler;
  private readonly ILogger<ChapterWriter> _logger;

  public ChapterWriter(
      IPromptBuilder promptBuilder,
      IStoryGenerationService storyGeneration,
      IChapterImageJobScheduler imageJobScheduler,
      ILogger<ChapterWriter> logger)
  {
    _promptBuilder = promptBuilder;
    _storyGeneration = storyGeneration;
    _imageJobScheduler = imageJobScheduler;
    _logger = logger;
  }

  public async Task WriteAsync(
      AdventureSession session,
      ThemeDefinition theme,
      ChapterTransition transition,
      CancellationToken cancellationToken)
  {
    try
    {
      var prompt = _promptBuilder.BuildChapterPrompt(new ChapterGenerationRequest(theme, session, transition));
      var (generated, chapter) = await GenerateValidChapterAsync(prompt, transition, cancellationToken);

      session.CompleteTransition(transition, chapter);
      session.UpdateMemory(generated.UpdatedSummary, generated.KeyFacts);
      if (chapter.Number == 1)
      {
        session.SetStoryOutline(generated.StoryOutline);
        session.SetTitleIfMissing(generated.Title);
      }

      _imageJobScheduler.ScheduleForChapter(session, chapter.Number, theme, generated.ImagePrompt);
    }
    catch
    {
      // Sans effet si la transition a déjà été validée ; sinon l'action pourra être retentée.
      session.AbandonTransition(transition);
      throw;
    }
  }

  private async Task<(GeneratedChapter Generated, AdventureChapter Chapter)> GenerateValidChapterAsync(
      StoryPrompt prompt,
      ChapterTransition transition,
      CancellationToken cancellationToken)
  {
    Exception? lastError = null;

    for (var attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      try
      {
        var generated = await _storyGeneration.GenerateChapterAsync(prompt, cancellationToken);
        var chapter = TryCreateChapter(generated, transition);
        if (chapter is not null)
        {
          return (generated, chapter);
        }

        _logger.LogWarning(
            "Chapitre {ChapterNumber} ({Kind}) généré avec une forme invalide (tentative {Attempt}/{MaxAttempts})",
            transition.NextChapterNumber,
            transition.NextKind,
            attempt,
            MaxAttempts);
      }
      catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
      {
        lastError = ex;
        _logger.LogWarning(
            ex,
            "Échec de génération du chapitre {ChapterNumber} (tentative {Attempt}/{MaxAttempts})",
            transition.NextChapterNumber,
            attempt,
            MaxAttempts);
      }
    }

    throw lastError is null
        ? new StoryGenerationException(_generationFailedMessage)
        : new StoryGenerationException(_generationFailedMessage, lastError);
  }

  /// <summary>
  /// Convertit la réponse du LLM en chapitre du domaine, ou <c>null</c> si elle est inexploitable.
  /// Les actions sont dédoublonnées et réidentifiées (« a », « b », « c ») : seuls leurs libellés comptent.
  /// </summary>
  private static AdventureChapter? TryCreateChapter(GeneratedChapter generated, ChapterTransition transition)
  {
    var text = generated.Text?.Trim();
    if (string.IsNullOrEmpty(text))
    {
      return null;
    }

    IReadOnlyCollection<ChapterChoice> choices = [];
    if (transition.NextKind == ChapterKind.Decision)
    {
      var labels = (generated.Choices ?? [])
          .Select(c => c.Label?.Trim())
          .OfType<string>()
          .Where(l => l.Length is > 0 and <= ChapterChoice.MaxLabelLength)
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .Take(AdventureChapter.MaxDecisionChoices)
          .ToArray();

      if (labels.Length < AdventureChapter.MinDecisionChoices)
      {
        return null;
      }

      choices = [.. labels.Select((label, index) => new ChapterChoice(_choiceIds[index], label))];
    }

    try
    {
      return new AdventureChapter(transition.NextChapterNumber, transition.NextKind, text, choices);
    }
    catch (DomainException)
    {
      return null;
    }
  }
}
