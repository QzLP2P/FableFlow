using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>Une scène de l'aventure : narration, illustration éventuelle et choix.</summary>
public sealed class AdventureScene
{
  private readonly List<SceneChoice> _choices;

  public AdventureScene(
      int sceneNumber,
      string text,
      IReadOnlyCollection<SceneChoice> choices,
      string? imageUrl = null)
  {
    if (sceneNumber < 1)
    {
      throw new DomainException("Le numéro de scène doit être supérieur ou égal à 1.");
    }

    if (string.IsNullOrWhiteSpace(text))
    {
      throw new DomainException("Le texte de la scène est requis.");
    }

    ArgumentNullException.ThrowIfNull(choices);

    Id = Guid.NewGuid();
    SceneNumber = sceneNumber;
    Text = text;
    ImageUrl = imageUrl;
    _choices = [.. choices];
  }

  public Guid Id { get; }

  public int SceneNumber { get; }

  public string Text { get; }

  public string? ImageUrl { get; private set; }

  /// <summary>Choix restant à jouer. Vide pour une scène de fin.</summary>
  public IReadOnlyList<SceneChoice> Choices => _choices;

  /// <summary>Identifiant du choix sélectionné par l'utilisateur, une fois joué.</summary>
  public string? SelectedChoiceId { get; private set; }

  public bool IsPlayed => SelectedChoiceId is not null;

  /// <summary>Récupère un choix par son identifiant, ou <c>null</c> s'il n'existe pas.</summary>
  public SceneChoice? FindChoice(string choiceId) =>
      _choices.FirstOrDefault(c => string.Equals(c.Id, choiceId, StringComparison.OrdinalIgnoreCase));

  internal void MarkChoicePlayed(string choiceId)
  {
    if (IsPlayed)
    {
      throw new DomainException("Cette scène a déjà été jouée.");
    }

    SelectedChoiceId = choiceId;
  }

  internal void AttachImage(string imageUrl) => ImageUrl = imageUrl;
}
