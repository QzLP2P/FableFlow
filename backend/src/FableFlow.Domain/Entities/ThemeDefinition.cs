using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Définition d'un univers narratif et de ses garde-fous de contenu.
/// Fournie par la couche Infrastructure (univers codés en dur au MVP, base de données ensuite).
/// Le vocabulaire et la densité ne dépendent pas de l'univers mais de l'âge du héros et du mode
/// de lecture (voir <c>NarrationPolicy</c>).
/// </summary>
public sealed class ThemeDefinition
{
  public ThemeDefinition(
      string id,
      string displayName,
      string description,
      AudienceTarget audience,
      string narrativeUniverse,
      IReadOnlyList<string> safetyConstraints,
      string imageStyle,
      IReadOnlyList<string>? recurringStoryBeats = null)
  {
    Id = id;
    DisplayName = displayName;
    Description = description;
    Audience = audience;
    NarrativeUniverse = narrativeUniverse;
    SafetyConstraints = safetyConstraints;
    ImageStyle = imageStyle;
    RecurringStoryBeats = recurringStoryBeats ?? [];
  }

  public string Id { get; }

  public string DisplayName { get; }

  /// <summary>Courte présentation de l'univers affichée au parent (sans divulgation d'intrigue).</summary>
  public string Description { get; }

  public AudienceTarget Audience { get; }

  /// <summary>
  /// Description de l'univers narratif (cadre, ton, personnages secondaires). Le héros est toujours
  /// l'enfant de l'histoire : un univers ne doit jamais imposer son propre protagoniste.
  /// </summary>
  public string NarrativeUniverse { get; }

  /// <summary>Contraintes de sécurité de contenu à injecter dans les prompts.</summary>
  public IReadOnlyList<string> SafetyConstraints { get; }

  /// <summary>Style artistique à appliquer aux images générées.</summary>
  public string ImageStyle { get; }

  /// <summary>
  /// Situations caractéristiques de l'univers, à piocher partiellement à chaque chapitre pour
  /// ancrer l'histoire dans un cadre reconnaissable plutôt que dans une péripétie interchangeable.
  /// </summary>
  public IReadOnlyList<string> RecurringStoryBeats { get; }

  public bool IsForChildren => Audience == AudienceTarget.Child;
}
