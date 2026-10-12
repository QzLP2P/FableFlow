using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Policies;

/// <summary>
/// Calendrier déterministe des chapitres d'une histoire. Le nombre total de chapitres inclut la
/// conclusion ; le premier et le dernier chapitre ne proposent jamais de choix. Ce calendrier est
/// la seule source de vérité de la progression : ni l'interface ni le modèle ne le décident.
/// </summary>
public sealed class ChapterSchedule
{
  private static readonly ChapterSchedule _short = new(StoryLength.Short, totalChapters: 6, decisionChapters: [2, 4]);
  private static readonly ChapterSchedule _longer = new(StoryLength.Longer, totalChapters: 10, decisionChapters: [3, 5, 8]);

  private ChapterSchedule(StoryLength length, int totalChapters, IReadOnlyList<int> decisionChapters)
  {
    Length = length;
    TotalChapters = totalChapters;
    DecisionChapters = decisionChapters;
  }

  public StoryLength Length { get; }

  /// <summary>Nombre total de chapitres, conclusion comprise.</summary>
  public int TotalChapters { get; }

  /// <summary>Numéros (1-based) des chapitres proposant un choix.</summary>
  public IReadOnlyList<int> DecisionChapters { get; }

  public static ChapterSchedule For(StoryLength length) => length switch
  {
    StoryLength.Short => _short,
    StoryLength.Longer => _longer,
    _ => throw new DomainException($"Longueur d'histoire inconnue : {length}.")
  };

  /// <summary>Nature attendue du chapitre <paramref name="chapterNumber"/>.</summary>
  public ChapterKind KindOf(int chapterNumber)
  {
    if (chapterNumber < 1 || chapterNumber > TotalChapters)
    {
      throw new DomainException(
          $"Le chapitre {chapterNumber} est hors de l'histoire (1 à {TotalChapters}).");
    }

    if (chapterNumber == TotalChapters)
    {
      return ChapterKind.Ending;
    }

    return DecisionChapters.Contains(chapterNumber) ? ChapterKind.Decision : ChapterKind.Narrative;
  }
}
