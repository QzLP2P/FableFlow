using System.Threading;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FableFlow.Domain.Policies;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Racine d'agrégat d'une histoire personnalisée. Porte l'instantané immuable de la configuration
/// (héros, mode de lecture, longueur, idée de départ) et la progression chapitre par chapitre selon
/// un calendrier déterministe. L'histoire se termine toujours par sa conclusion narrative : il n'y a
/// ni victoire ni défaite.
/// </summary>
public sealed class AdventureSession
{
  public const int MaxPremiseLength = 600;
  public const int MaxTitleLength = 120;
  public const int MaxKeyFacts = 12;

  // Protège l'état contre les accès concurrents : les illustrations sont attachées en arrière-plan
  // pendant que des requêtes HTTP lisent l'histoire ou la font progresser.
  private readonly Lock _gate = new();

  private readonly List<AdventureChapter> _chapters = [];
  private IReadOnlyList<string> _keyFacts = [];
  private IReadOnlyList<string> _storyOutline = [];
  private ChapterTransition? _pendingTransition;

  private AdventureSession(
      Guid id,
      string themeId,
      HeroProfile hero,
      ReaderMode readerMode,
      ChapterSchedule schedule,
      string premise,
      string? title)
  {
    Id = id;
    ThemeId = themeId;
    Hero = hero;
    ReaderMode = readerMode;
    Schedule = schedule;
    Premise = premise;
    Title = title;
    Status = SessionStatus.InProgress;
    RunningSummary = string.Empty;
  }

  public Guid Id { get; }

  public string ThemeId { get; }

  /// <summary>Instantané du héros pris au démarrage.</summary>
  public HeroProfile Hero { get; }

  public ReaderMode ReaderMode { get; }

  public ChapterSchedule Schedule { get; }

  public StoryLength Length => Schedule.Length;

  public int TotalChapters => Schedule.TotalChapters;

  /// <summary>Idée de départ choisie (proposition générée ou idée personnelle).</summary>
  public string Premise { get; }

  /// <summary>Titre de l'histoire, issu de la proposition choisie ou fixé à l'ouverture.</summary>
  public string? Title { get; private set; }

  public SessionStatus Status { get; private set; }

  public bool IsCompleted => Status == SessionStatus.Completed;

  /// <summary>Résumé cumulatif alimentant la mémoire narrative.</summary>
  public string RunningSummary { get; private set; }

  /// <summary>Faits clés à ne pas contredire (personnages, objets, lieux).</summary>
  public IReadOnlyList<string> KeyFacts
  {
    get { lock (_gate) { return _keyFacts; } }
  }

  /// <summary>
  /// Plan d'ensemble fixé à l'ouverture, fil conducteur de la génération. Usage interne :
  /// jamais exposé au client (pas de divulgation de l'intrigue).
  /// </summary>
  public IReadOnlyList<string> StoryOutline
  {
    get { lock (_gate) { return _storyOutline; } }
  }

  /// <summary>Copie des chapitres déjà écrits, dans l'ordre.</summary>
  public IReadOnlyList<AdventureChapter> Chapters
  {
    get { lock (_gate) { return [.. _chapters]; } }
  }

  /// <summary>Chapitre le plus avancé (la « frontière » de lecture), ou <c>null</c> avant l'ouverture.</summary>
  public AdventureChapter? CurrentChapter
  {
    get { lock (_gate) { return _chapters.Count == 0 ? null : _chapters[^1]; } }
  }

  public int CurrentChapterNumber
  {
    get { lock (_gate) { return _chapters.Count; } }
  }

  /// <summary>Indique qu'un chapitre est en cours de génération.</summary>
  public bool IsGenerating
  {
    get { lock (_gate) { return _pendingTransition is not null; } }
  }

  public static AdventureSession Start(
      Guid id,
      string themeId,
      HeroProfile hero,
      ReaderMode readerMode,
      StoryLength length,
      string premise,
      string? title = null)
  {
    if (string.IsNullOrWhiteSpace(themeId))
    {
      throw new DomainException("L'univers est requis pour démarrer une histoire.");
    }

    ArgumentNullException.ThrowIfNull(hero);

    if (!Enum.IsDefined(readerMode))
    {
      throw new DomainException($"Mode de lecture inconnu : {readerMode}.");
    }

    if (string.IsNullOrWhiteSpace(premise))
    {
      throw new DomainException("Une idée d'histoire est requise.");
    }

    var normalizedPremise = premise.Trim();
    if (normalizedPremise.Length > MaxPremiseLength)
    {
      throw new DomainException($"L'idée d'histoire ne doit pas dépasser {MaxPremiseLength} caractères.");
    }

    var normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    if (normalizedTitle is not null && normalizedTitle.Length > MaxTitleLength)
    {
      throw new DomainException($"Le titre ne doit pas dépasser {MaxTitleLength} caractères.");
    }

    return new AdventureSession(
        id,
        themeId,
        hero,
        readerMode,
        ChapterSchedule.For(length),
        normalizedPremise,
        normalizedTitle);
  }

  /// <summary>Réserve l'écriture du premier chapitre.</summary>
  public ChapterTransition BeginOpening()
  {
    lock (_gate)
    {
      EnsureNoPendingTransition();
      if (_chapters.Count > 0)
      {
        throw new StoryConflictException("L'histoire a déjà commencé.");
      }

      return Reserve(fromChapterNumber: 0, selectedChoice: null);
    }
  }

  /// <summary>Réserve le passage au chapitre suivant depuis un chapitre de récit.</summary>
  /// <param name="fromChapterNumber">Chapitre courant vu par le lecteur (détecte les doublons).</param>
  public ChapterTransition BeginAdvance(int fromChapterNumber)
  {
    lock (_gate)
    {
      var frontier = EnsureCanActOn(fromChapterNumber);
      if (frontier.Kind == ChapterKind.Decision)
      {
        throw new StoryConflictException("Ce chapitre attend un choix avant de continuer.");
      }

      return Reserve(fromChapterNumber, selectedChoice: null);
    }
  }

  /// <summary>Réserve la suite de l'histoire après un choix fait sur un moment de choix.</summary>
  public ChapterTransition BeginChoice(int fromChapterNumber, string choiceId)
  {
    lock (_gate)
    {
      var frontier = EnsureCanActOn(fromChapterNumber);
      if (frontier.Kind != ChapterKind.Decision)
      {
        throw new StoryConflictException("Ce chapitre ne propose pas de choix : passez au chapitre suivant.");
      }

      var choice = frontier.FindChoice(choiceId)
          ?? throw new DomainException($"Le choix '{choiceId}' n'existe pas dans ce chapitre.");

      return Reserve(fromChapterNumber, choice);
    }
  }

  /// <summary>
  /// Valide la transition réservée avec le chapitre généré : enregistre le choix éventuel, ajoute
  /// le chapitre et termine l'histoire si c'est la conclusion. Le choix n'est enregistré qu'ici,
  /// une fois le chapitre suivant disponible, pour qu'un échec de génération ne le consomme pas.
  /// </summary>
  public void CompleteTransition(ChapterTransition transition, AdventureChapter chapter)
  {
    ArgumentNullException.ThrowIfNull(transition);
    ArgumentNullException.ThrowIfNull(chapter);

    lock (_gate)
    {
      if (_pendingTransition is null || _pendingTransition.Token != transition.Token)
      {
        throw new StoryConflictException("Cette écriture de chapitre n'est plus attendue.");
      }

      if (chapter.Number != transition.NextChapterNumber || chapter.Kind != transition.NextKind)
      {
        throw new DomainException(
            $"Chapitre incohérent : attendu {transition.NextChapterNumber} ({transition.NextKind}), reçu {chapter.Number} ({chapter.Kind}).");
      }

      if (transition.SelectedChoice is not null)
      {
        _chapters[^1].RecordChoice(transition.SelectedChoice.Id);
      }

      _chapters.Add(chapter);
      if (chapter.Kind == ChapterKind.Ending)
      {
        Status = SessionStatus.Completed;
      }

      _pendingTransition = null;
    }
  }

  /// <summary>Libère une réservation après un échec : l'action pourra être retentée.</summary>
  public void AbandonTransition(ChapterTransition transition)
  {
    ArgumentNullException.ThrowIfNull(transition);

    lock (_gate)
    {
      if (_pendingTransition?.Token == transition.Token)
      {
        _pendingTransition = null;
      }
    }
  }

  /// <summary>Met à jour la mémoire narrative ; les valeurs vides conservent l'existant.</summary>
  public void UpdateMemory(string? summary, IEnumerable<string>? keyFacts)
  {
    lock (_gate)
    {
      if (!string.IsNullOrWhiteSpace(summary))
      {
        RunningSummary = summary.Trim();
      }

      var facts = (keyFacts ?? [])
          .Where(f => !string.IsNullOrWhiteSpace(f))
          .Select(f => f.Trim())
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .Take(MaxKeyFacts)
          .ToArray();

      if (facts.Length > 0)
      {
        _keyFacts = facts;
      }
    }
  }

  /// <summary>Fixe le plan d'ensemble à l'ouverture ; ignoré s'il est vide ou déjà fixé.</summary>
  public void SetStoryOutline(IReadOnlyList<string>? outline)
  {
    var steps = (outline ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
    if (steps.Length == 0)
    {
      return;
    }

    lock (_gate)
    {
      if (_storyOutline.Count == 0)
      {
        _storyOutline = steps;
      }
    }
  }

  /// <summary>Fixe le titre s'il n'a pas été choisi au démarrage (idée personnelle).</summary>
  public void SetTitleIfMissing(string? title)
  {
    if (string.IsNullOrWhiteSpace(title))
    {
      return;
    }

    lock (_gate)
    {
      if (Title is null)
      {
        var trimmed = title.Trim();
        Title = trimmed.Length > MaxTitleLength ? trimmed[..MaxTitleLength].TrimEnd() : trimmed;
      }
    }
  }

  public void MarkIllustrationPending(int chapterNumber) =>
      WithChapter(chapterNumber, c => c.MarkIllustrationPending());

  public void AttachIllustration(int chapterNumber, string imageUrl)
  {
    if (string.IsNullOrWhiteSpace(imageUrl))
    {
      MarkIllustrationFailed(chapterNumber);
      return;
    }

    WithChapter(chapterNumber, c => c.AttachIllustration(imageUrl));
  }

  public void MarkIllustrationFailed(int chapterNumber) =>
      WithChapter(chapterNumber, c => c.MarkIllustrationFailed());

  private void WithChapter(int chapterNumber, Action<AdventureChapter> action)
  {
    lock (_gate)
    {
      var chapter = _chapters.FirstOrDefault(c => c.Number == chapterNumber);
      if (chapter is not null)
      {
        action(chapter);
      }
    }
  }

  private AdventureChapter EnsureCanActOn(int fromChapterNumber)
  {
    if (IsCompleted)
    {
      throw new StoryConflictException("L'histoire est terminée.");
    }

    EnsureNoPendingTransition();

    if (_chapters.Count == 0)
    {
      throw new StoryConflictException("L'histoire n'a pas encore commencé.");
    }

    var frontier = _chapters[^1];
    if (fromChapterNumber != frontier.Number)
    {
      throw new StoryConflictException(
          $"Le chapitre {fromChapterNumber} n'est pas le chapitre courant (chapitre courant : {frontier.Number}).");
    }

    return frontier;
  }

  private void EnsureNoPendingTransition()
  {
    if (_pendingTransition is not null)
    {
      throw new StoryConflictException("Le chapitre suivant est déjà en cours d'écriture.");
    }
  }

  private ChapterTransition Reserve(int fromChapterNumber, ChapterChoice? selectedChoice)
  {
    var next = fromChapterNumber + 1;
    _pendingTransition = new ChapterTransition(fromChapterNumber, next, Schedule.KindOf(next), selectedChoice);
    return _pendingTransition;
  }
}
