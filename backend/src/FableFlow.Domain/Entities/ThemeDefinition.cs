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
      string imageStyle)
  {
    Id = id;
    DisplayName = displayName;
    Audience = audience;
    VocabularyLevel = vocabularyLevel;
    NarrativeUniverse = narrativeUniverse;
    SafetyConstraints = safetyConstraints;
    ImageStyle = imageStyle;
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

  public bool IsForChildren => Audience == AudienceTarget.Child;
}
