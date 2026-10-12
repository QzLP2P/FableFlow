using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>Action proposée au lecteur lors d'un moment de choix. Aucune n'est « bonne » ou « mauvaise ».</summary>
public sealed class ChapterChoice
{
  public const int MaxIdLength = 20;
  public const int MaxLabelLength = 200;

  public ChapterChoice(string id, string label)
  {
    if (string.IsNullOrWhiteSpace(id) || id.Length > MaxIdLength)
    {
      throw new DomainException($"L'identifiant du choix est requis ({MaxIdLength} caractères au plus).");
    }

    if (string.IsNullOrWhiteSpace(label) || label.Length > MaxLabelLength)
    {
      throw new DomainException($"Le libellé du choix est requis ({MaxLabelLength} caractères au plus).");
    }

    Id = id.Trim();
    Label = label.Trim();
  }

  /// <summary>Identifiant court (ex. "a", "b", "c").</summary>
  public string Id { get; }

  /// <summary>Libellé affiché au lecteur.</summary>
  public string Label { get; }
}
