using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Un chapitre de l'histoire. Sa forme est contrôlée par sa nature : un chapitre de récit ou de
/// conclusion n'a aucun choix, un moment de choix propose 2 à 3 actions distinctes.
/// </summary>
public sealed class AdventureChapter
{
  public const int MinDecisionChoices = 2;
  public const int MaxDecisionChoices = 3;

  private readonly List<ChapterChoice> _choices;

  public AdventureChapter(
      int number,
      ChapterKind kind,
      string text,
      IReadOnlyCollection<ChapterChoice> choices)
  {
    if (number < 1)
    {
      throw new DomainException("Le numéro de chapitre doit être supérieur ou égal à 1.");
    }

    if (string.IsNullOrWhiteSpace(text))
    {
      throw new DomainException("Le texte du chapitre est requis.");
    }

    ArgumentNullException.ThrowIfNull(choices);
    EnsureShapeMatchesKind(kind, choices);

    Number = number;
    Kind = kind;
    Text = text.Trim();
    _choices = [.. choices];
    IllustrationStatus = IllustrationStatus.NotRequested;
  }

  public int Number { get; }

  public ChapterKind Kind { get; }

  public string Text { get; }

  /// <summary>Actions proposées ; vide hors moment de choix.</summary>
  public IReadOnlyList<ChapterChoice> Choices => _choices;

  /// <summary>Action retenue par le lecteur, une fois le chapitre suivant généré.</summary>
  public string? SelectedChoiceId { get; private set; }

  public ChapterChoice? SelectedChoice =>
      SelectedChoiceId is null ? null : FindChoice(SelectedChoiceId);

  public IllustrationStatus IllustrationStatus { get; private set; }

  public string? ImageUrl { get; private set; }

  public ChapterChoice? FindChoice(string choiceId) =>
      _choices.FirstOrDefault(c => string.Equals(c.Id, choiceId, StringComparison.OrdinalIgnoreCase));

  internal void RecordChoice(string choiceId)
  {
    if (Kind != ChapterKind.Decision)
    {
      throw new StoryConflictException("Ce chapitre ne propose pas de choix.");
    }

    if (SelectedChoiceId is not null)
    {
      throw new StoryConflictException("Un choix a déjà été fait pour ce chapitre.");
    }

    SelectedChoiceId = FindChoice(choiceId)?.Id
        ?? throw new DomainException($"Le choix '{choiceId}' n'existe pas dans ce chapitre.");
  }

  internal void MarkIllustrationPending()
  {
    if (IllustrationStatus == IllustrationStatus.NotRequested)
    {
      IllustrationStatus = IllustrationStatus.Pending;
    }
  }

  internal void AttachIllustration(string imageUrl)
  {
    ImageUrl = imageUrl;
    IllustrationStatus = IllustrationStatus.Ready;
  }

  internal void MarkIllustrationFailed()
  {
    if (IllustrationStatus != IllustrationStatus.Ready)
    {
      IllustrationStatus = IllustrationStatus.Failed;
    }
  }

  private static void EnsureShapeMatchesKind(ChapterKind kind, IReadOnlyCollection<ChapterChoice> choices)
  {
    if (kind != ChapterKind.Decision)
    {
      if (choices.Count != 0)
      {
        throw new DomainException("Seul un moment de choix peut proposer des actions.");
      }

      return;
    }

    if (choices.Count is < MinDecisionChoices or > MaxDecisionChoices)
    {
      throw new DomainException(
          $"Un moment de choix doit proposer entre {MinDecisionChoices} et {MaxDecisionChoices} actions.");
    }

    var distinctIds = choices.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    var distinctLabels = choices.Select(c => c.Label).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    if (distinctIds != choices.Count || distinctLabels != choices.Count)
    {
      throw new DomainException("Les actions d'un moment de choix doivent être distinctes.");
    }
  }
}
