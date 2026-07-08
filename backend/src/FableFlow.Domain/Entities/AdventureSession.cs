using System.Threading;
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

  // Protège les scènes et l'issue contre les accès concurrents : la génération d'image se termine
  // désormais en arrière-plan (voir ISceneImageJobScheduler côté Application) et peut donc écrire sur
  // cette session au même moment qu'une requête HTTP en cours (nouveau choix, polling de l'état).
  private readonly Lock _gate = new();

  private readonly List<AdventureScene> _scenes = [];

  private AdventureSession(Guid id, string themeId, int targetSceneCount, int maxBadChoices, string? narrativePremise)
  {
    Id = id;
    ThemeId = themeId;
    TargetSceneCount = targetSceneCount;
    MaxBadChoices = maxBadChoices;
    NarrativePremise = narrativePremise;
    Status = SessionStatus.InProgress;
    RunningSummary = string.Empty;
    CurrentSceneNumber = 0;
  }

  public Guid Id { get; }

  public string ThemeId { get; }

  public int TargetSceneCount { get; }

  public int MaxBadChoices { get; }

  /// <summary>
  /// Axe narratif principal choisi par l'utilisateur parmi les propositions générées
  /// (voir <c>GetStoryPremisesQuery</c>). Sert d'ancrage pour toutes les scènes générées
  /// tout au long de l'aventure. <c>null</c> si l'utilisateur n'a pas choisi d'axe (le LLM
  /// improvise librement dans ce cas).
  /// </summary>
  public string? NarrativePremise { get; }

  public int CurrentSceneNumber { get; private set; }

  public int BadChoiceCount { get; private set; }

  public SessionStatus Status { get; private set; }

  /// <summary>Résumé courant alimentant la mémoire narrative.</summary>
  public string RunningSummary { get; private set; }

  /// <summary>
  /// Plan d'ensemble de l'aventure (quelques grandes étapes/actes, pas une par scène), généré une
  /// fois avec la scène initiale et utilisé comme fil conducteur pour toutes les scènes suivantes
  /// afin de garantir une progression cohérente sur toute la durée de l'aventure. Vide tant qu'il
  /// n'a pas encore été fixé (ou si le LLM n'en a pas fourni). Usage interne à la génération
  /// narrative uniquement : jamais exposé au client (pas de spoiler de l'histoire).
  /// </summary>
  public IReadOnlyList<string> StoryOutline { get; private set; } = [];

  public AdventureOutcome? Outcome { get; private set; }

  /// <summary>Instantané des scènes ; une copie est retournée à chaque accès pour rester sûre en cas d'écriture concurrente.</summary>
  public IReadOnlyList<AdventureScene> Scenes
  {
    get { lock (_gate) { return [.. _scenes]; } }
  }

  public AdventureScene? CurrentScene
  {
    get { lock (_gate) { return GetCurrentSceneNoLock(); } }
  }

  public bool IsFinished => Status != SessionStatus.InProgress;

  private AdventureScene? GetCurrentSceneNoLock() => _scenes.Count == 0 ? null : _scenes[^1];

  /// <summary>Démarre une nouvelle aventure.</summary>
  public static AdventureSession Start(
      Guid id,
      string themeId,
      int targetSceneCount,
      int maxBadChoices = DefaultMaxBadChoices,
      string? narrativePremise = null)
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

    return new AdventureSession(id, themeId, targetSceneCount, maxBadChoices, narrativePremise);
  }

  /// <summary>Rattache la scène nouvellement générée et fait progresser le compteur de scène.</summary>
  public void AttachScene(AdventureScene scene)
  {
    ArgumentNullException.ThrowIfNull(scene);

    lock (_gate)
    {
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
  }

  /// <summary>
  /// Applique le choix de l'utilisateur sur la scène courante et met à jour le statut.
  /// Retourne un résultat indiquant si une scène suivante doit être générée.
  /// </summary>
  public ChoiceApplicationResult RecordChoice(string choiceId)
  {
    lock (_gate)
    {
      if (IsFinished)
      {
        throw new DomainException("L'aventure est déjà terminée.");
      }

      var scene = GetCurrentSceneNoLock()
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
  }

  /// <summary>Met à jour la mémoire narrative après une génération.</summary>
  public void UpdateSummary(string summary)
  {
    lock (_gate)
    {
      RunningSummary = summary ?? string.Empty;
    }
  }

  /// <summary>
  /// Fixe le plan d'ensemble généré avec la scène initiale (voir <see cref="StoryOutline"/>). Ne
  /// fait rien si le plan fourni est vide (le LLM n'en a pas produit, ou appel pour une scène de
  /// continuation/fin qui ne doit pas en générer).
  /// </summary>
  public void SetStoryOutline(IReadOnlyList<string> outline)
  {
    if (outline is null || outline.Count == 0)
    {
      return;
    }

    lock (_gate)
    {
      StoryOutline = [.. outline];
    }
  }

  /// <summary>Fixe l'issue finale de l'aventure terminée.</summary>
  public void SetOutcome(AdventureOutcome outcome)
  {
    ArgumentNullException.ThrowIfNull(outcome);

    lock (_gate)
    {
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
  }

  /// <summary>
  /// Illustre une scène identifiée par son numéro (pas nécessairement la scène courante : la
  /// génération d'image est désormais asynchrone et peut se terminer après que l'utilisateur a déjà
  /// fait progresser l'aventure vers une scène suivante). Ne fait rien si la scène n'existe plus (déjà
  /// purgée) ou si l'URL est vide.
  /// </summary>
  public void AttachImageToScene(int sceneNumber, string imageUrl)
  {
    if (string.IsNullOrWhiteSpace(imageUrl))
    {
      return;
    }

    lock (_gate)
    {
      var scene = _scenes.FirstOrDefault(s => s.SceneNumber == sceneNumber);
      scene?.AttachImage(imageUrl);
    }
  }

  /// <summary>
  /// Illustre l'issue déjà fixée par <see cref="SetOutcome"/> (image générée a posteriori en
  /// arrière-plan). Ne fait rien si aucune issue n'est encore fixée ou si l'URL est vide.
  /// </summary>
  public void AttachImageToOutcome(string imageUrl)
  {
    if (string.IsNullOrWhiteSpace(imageUrl))
    {
      return;
    }

    lock (_gate)
    {
      if (Outcome is not null)
      {
        Outcome = Outcome.WithImage(imageUrl);
      }
    }
  }
}
