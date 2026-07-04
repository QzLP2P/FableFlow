namespace FableFlow.Infrastructure.Ai.Contracts;

/// <summary>Proposition d'axe narratif telle que retournée brute par le LLM.</summary>
public sealed class RawGeneratedPremise
{
  public string Title { get; set; } = string.Empty;

  public string Hook { get; set; } = string.Empty;
}

/// <summary>
/// Forme JSON attendue de la réponse du LLM pour la génération de propositions d'axe narratif
/// (voir <see cref="Prompts.PromptTemplateRegistry.PremiseSystemPrompt"/>).
/// </summary>
public sealed class RawPremisesResponse
{
  public List<RawGeneratedPremise> Premises { get; set; } = [];
}
