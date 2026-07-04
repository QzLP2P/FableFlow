namespace FableFlow.Infrastructure.Ai.Contracts;

/// <summary>Choix tel que retourné brut par le LLM, avant validation.</summary>
public sealed class RawGeneratedChoice
{
  public string Id { get; set; } = string.Empty;

  public string Label { get; set; } = string.Empty;

  public string Outcome { get; set; } = "Neutral";
}

/// <summary>
/// Forme JSON attendue de la réponse du LLM (voir <see cref="Prompts.PromptTemplateRegistry.SceneSystemPrompt"/>).
/// </summary>
public sealed class RawGeneratedScene
{
  public string Text { get; set; } = string.Empty;

  public List<RawGeneratedChoice> Choices { get; set; } = [];

  public string UpdatedSummary { get; set; } = string.Empty;

  public List<string> KeyFacts { get; set; } = [];
}
