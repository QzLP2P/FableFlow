using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>Un choix proposé à l'utilisateur au sein d'une scène.</summary>
public sealed class SceneChoice
{
  public SceneChoice(string id, string label, ChoiceOutcome outcome)
  {
    if (string.IsNullOrWhiteSpace(id))
    {
      throw new DomainException("L'identifiant du choix est requis.");
    }

    if (string.IsNullOrWhiteSpace(label))
    {
      throw new DomainException("Le libellé du choix est requis.");
    }

    Id = id;
    Label = label;
    Outcome = outcome;
  }

  /// <summary>Identifiant court du choix (ex. "a", "b", "c").</summary>
  public string Id { get; }

  /// <summary>Libellé affiché à l'utilisateur.</summary>
  public string Label { get; }

  /// <summary>Impact narratif du choix (neutre, bon, mauvais).</summary>
  public ChoiceOutcome Outcome { get; }
}
