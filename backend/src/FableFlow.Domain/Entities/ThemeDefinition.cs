using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Définition d'un thème narratif et de ses garde-fous de contenu.
/// Fournie par la couche Infrastructure (thèmes codés en dur au MVP, base de données ensuite).
/// </summary>
public sealed class ThemeDefinition
{
  public ThemeDefinition(
      string id,
      string displayName,
      AudienceTarget audience,
      VocabularyLevel vocabularyLevel,
      string narrativeUniverse,
      IReadOnlyList<string> safetyConstraints,
      string imageStyle,
      IReadOnlyList<string>? recurringStoryBeats = null)
  {
    Id = id;
    DisplayName = displayName;
    Audience = audience;
    VocabularyLevel = vocabularyLevel;
    NarrativeUniverse = narrativeUniverse;
    SafetyConstraints = safetyConstraints;
    ImageStyle = imageStyle;
    RecurringStoryBeats = recurringStoryBeats ?? [];
  }

  public string Id { get; }

  public string DisplayName { get; }

  public AudienceTarget Audience { get; }

  public VocabularyLevel VocabularyLevel { get; }

  /// <summary>Description de l'univers narratif (cadre, ton, personnages types).</summary>
  public string NarrativeUniverse { get; }

  /// <summary>Contraintes de sécurité de contenu à injecter dans les prompts.</summary>
  public IReadOnlyList<string> SafetyConstraints { get; }

  /// <summary>Style artistique à appliquer aux images générées.</summary>
  public string ImageStyle { get; }

  /// <summary>
  /// Situations ou péripéties caractéristiques de l'univers d'origine (ex. capturer un Pokémon,
  /// affronter un vilain récurrent...), à piocher partiellement à chaque scène pour rapprocher
  /// l'aventure générée du matériau source plutôt que d'une péripétie générique interchangeable
  /// entre thèmes. Injectées dans les prompts par la couche Infrastructure.
  /// </summary>
  public IReadOnlyList<string> RecurringStoryBeats { get; }

  public bool IsForChildren => Audience == AudienceTarget.Child;
}
