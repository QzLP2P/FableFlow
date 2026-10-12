using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Instantané immuable du héros d'une histoire (l'enfant), copié dans la session au démarrage :
/// modifier ou supprimer le profil enregistré sur l'appareil ne réécrit jamais une histoire existante.
/// Les bornes sont partagées avec la validation des entrées (API) et l'interface.
/// </summary>
public sealed class HeroProfile
{
  public const int MinAge = 3;
  public const int MaxAge = 12;
  public const int MaxNameLength = 40;
  public const int MaxCompanions = 3;
  public const int MaxCompanionLength = 60;
  public const int MaxDetailLength = 200;

  private HeroProfile(
      string name,
      int age,
      IReadOnlyList<string> companions,
      string? interests,
      string? personality,
      string? appearance)
  {
    Name = name;
    Age = age;
    Companions = companions;
    Interests = interests;
    Personality = personality;
    Appearance = appearance;
  }

  /// <summary>Prénom du héros.</summary>
  public string Name { get; }

  /// <summary>Âge en années, utilisé pour adapter la narration.</summary>
  public int Age { get; }

  /// <summary>Compagnons facultatifs (ex. « Max, son chien »).</summary>
  public IReadOnlyList<string> Companions { get; }

  /// <summary>Centres d'intérêt facultatifs.</summary>
  public string? Interests { get; }

  /// <summary>Traits de caractère facultatifs.</summary>
  public string? Personality { get; }

  /// <summary>Description d'apparence facultative (aucune promesse de ressemblance photographique).</summary>
  public string? Appearance { get; }

  /// <summary>Crée un héros après normalisation (espaces superflus) et contrôle des bornes.</summary>
  public static HeroProfile Create(
      string name,
      int age,
      IEnumerable<string>? companions = null,
      string? interests = null,
      string? personality = null,
      string? appearance = null)
  {
    var normalizedName = Normalize(name)
        ?? throw new DomainException("Le prénom du héros est requis.");

    if (normalizedName.Length > MaxNameLength)
    {
      throw new DomainException($"Le prénom du héros ne doit pas dépasser {MaxNameLength} caractères.");
    }

    if (age is < MinAge or > MaxAge)
    {
      throw new DomainException($"L'âge du héros doit être compris entre {MinAge} et {MaxAge} ans.");
    }

    var normalizedCompanions = (companions ?? [])
        .Select(Normalize)
        .OfType<string>()
        .ToArray();

    if (normalizedCompanions.Length > MaxCompanions)
    {
      throw new DomainException($"Un héros peut avoir au plus {MaxCompanions} compagnons.");
    }

    if (normalizedCompanions.Any(c => c.Length > MaxCompanionLength))
    {
      throw new DomainException($"Chaque compagnon doit faire au plus {MaxCompanionLength} caractères.");
    }

    return new HeroProfile(
        normalizedName,
        age,
        normalizedCompanions,
        NormalizeDetail(interests, "centres d'intérêt"),
        NormalizeDetail(personality, "traits de caractère"),
        NormalizeDetail(appearance, "apparence"));
  }

  private static string? NormalizeDetail(string? value, string label)
  {
    var normalized = Normalize(value);
    if (normalized is not null && normalized.Length > MaxDetailLength)
    {
      throw new DomainException($"Le champ « {label} » ne doit pas dépasser {MaxDetailLength} caractères.");
    }

    return normalized;
  }

  private static string? Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
  }
}
