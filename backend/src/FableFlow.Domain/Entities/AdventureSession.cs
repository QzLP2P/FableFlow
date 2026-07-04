using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Racine d'agrégat représentant une aventure en cours. Encapsule la logique
/// narrative : progression des scènes, comptage des mauvais choix et transitions
/// de statut (victoire / défaite / fin neutre).
/// </summary>
public sealed class AdventureSession
{
  public const int DefaultMaxBadChoices = 3;
  public const int MinSceneCount = 3;
  public const int MaxSceneCount = 20;

  private readonly List<AdventureScene> _scenes = [];

  private AdventureSession(Guid id, string themeId, int targetSceneCount, int maxBadChoices)
  {
    Id = id;
    ThemeId = themeId;
    TargetSceneCount = targetSceneCount;
    MaxBadChoices = maxBadChoices;
    Status = SessionStatus.InProgress;
    RunningSummary = string.Empty;
    CurrentSceneNumber = 0;
  }

  public Guid Id { get; }

  public string ThemeId { get; }

  public int TargetSceneCount { get; }

  public int MaxBadChoices { get; }

  public int CurrentSceneNumber { get; private set; }

  public int BadChoiceCount { get; private set; }

  public SessionStatus Status { get; private set; }

  /// <summary>Résumé courant alimentant la mémoire narrative.</summary>
  public string RunningSummary { get; private set; }

  public AdventureOutcome? Outcome { get; private set; }

  public IReadOnlyList<AdventureScene> Scenes => _scenes;

  public AdventureScene? CurrentScene => _scenes.Count == 0 ? null : _scenes[^1];

  public bool IsFinished => Status != SessionStatus.InProgress;

  /// <summary>Démarre une nouvelle aventure.</summary>
  public static AdventureSession Start(
      Guid id,
      string themeId,
      int targetSceneCount,
      int maxBadChoices = DefaultMaxBadChoices)
  {
    if (string.IsNullOrWhiteSpace(themeId))
    {
      throw new DomainException("Le thème est requis pour démarrer une aventure.");
    }

    if (targetSceneCount is < MinSceneCount or > MaxSceneCount)
    {
      throw new DomainException(
          $"Le nombre de scènes doit être compris entre {MinSceneCount} et {MaxSceneCount}.");
    }

    if (maxBadChoices < 1)
    {
      throw new DomainException("Le nombre maximum de mauvais choix doit être supérieur ou égal à 1.");
    }

    return new AdventureSession(id, themeId, targetSceneCount, maxBadChoices);
  }

  /// <summary>Rattache la scène nouvellement générée et fait progresser le compteur de scène.</summary>
  public void AttachScene(AdventureScene scene)
  {
    ArgumentNullException.ThrowIfNull(scene);

    if (IsFinished)
    {
      throw new DomainException("Impossible d'ajouter une scène à une aventure terminée.");
    }

    var expected = CurrentSceneNumber + 1;
    if (scene.SceneNumber != expected)
    {
      throw new DomainException(
          $"Scène incohérente : attendu {expected}, reçu {scene.SceneNumber}.");
    }

    _scenes.Add(scene);
    CurrentSceneNumber = scene.SceneNumber;
  }

  /// <summary>
  /// Applique le choix de l'utilisateur sur la scène courante et met à jour le statut.
  /// Retourne un résultat indiquant si une scène suivante doit être générée.
  /// </summary>
  public ChoiceApplicationResult RecordChoice(string choiceId)
  {
    if (IsFinished)
    {
      throw new DomainException("L'aventure est déjà terminée.");
    }

    var scene = CurrentScene
        ?? throw new DomainException("Aucune scène courante à jouer.");

    var choice = scene.FindChoice(choiceId)
        ?? throw new DomainException($"Le choix '{choiceId}' n'existe pas dans la scène courante.");

    scene.MarkChoicePlayed(choice.Id);

    if (choice.Outcome == ChoiceOutcome.Bad)
    {
      BadChoiceCount++;
    }

    if (BadChoiceCount >= MaxBadChoices)
    {
      Status = SessionStatus.Lost;
      return new ChoiceApplicationResult(Status, choice.Outcome, RequiresNextScene: false);
    }

    var reachedEnd = CurrentSceneNumber >= TargetSceneCount;
    if (reachedEnd)
    {
      Status = BadChoiceCount == 0 ? SessionStatus.Won : SessionStatus.Completed;
      return new ChoiceApplicationResult(Status, choice.Outcome, RequiresNextScene: false);
    }

    return new ChoiceApplicationResult(SessionStatus.InProgress, choice.Outcome, RequiresNextScene: true);
  }

  /// <summary>Met à jour la mémoire narrative après une génération.</summary>
  public void UpdateSummary(string summary) =>
      RunningSummary = summary ?? string.Empty;

  /// <summary>Fixe l'issue finale de l'aventure terminée.</summary>
  public void SetOutcome(AdventureOutcome outcome)
  {
    ArgumentNullException.ThrowIfNull(outcome);

    if (!IsFinished)
    {
      throw new DomainException("Impossible de fixer une issue tant que l'aventure est en cours.");
    }

    if (outcome.Status != Status)
    {
      throw new DomainException("L'issue ne correspond pas au statut terminal de la session.");
    }

    Outcome = outcome;
  }

  /// <summary>Illustre la scène courante (image générée a posteriori).</summary>
  public void AttachImageToCurrentScene(string imageUrl)
  {
    if (string.IsNullOrWhiteSpace(imageUrl))
    {
      return;
    }

    CurrentScene?.AttachImage(imageUrl);
  }
}
