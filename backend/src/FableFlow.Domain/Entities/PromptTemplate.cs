using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Gabarit de prompt versionné. Permet de faire évoluer les instructions LLM
/// sans casser la reproductibilité des générations antérieures.
/// </summary>
public sealed class PromptTemplate
{
  public PromptTemplate(string id, int version, string template)
  {
    if (string.IsNullOrWhiteSpace(id))
    {
      throw new DomainException("L'identifiant du gabarit est requis.");
    }

    if (version < 1)
    {
      throw new DomainException("La version du gabarit doit être supérieure ou égale à 1.");
    }

    if (string.IsNullOrWhiteSpace(template))
    {
      throw new DomainException("Le contenu du gabarit est requis.");
    }

    Id = id;
    Version = version;
    Template = template;
  }

  public string Id { get; }

  public int Version { get; }

  /// <summary>Contenu du gabarit, avec jetons de substitution (ex. <c>{{theme}}</c>).</summary>
  public string Template { get; }

  public string VersionedId => $"{Id}@v{Version}";
}
