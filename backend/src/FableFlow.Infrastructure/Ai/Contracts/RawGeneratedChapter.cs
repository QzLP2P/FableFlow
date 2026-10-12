namespace FableFlow.Infrastructure.Ai.Contracts;

/// <summary>Choix tel que retourné brut par le LLM, avant validation.</summary>
public sealed class RawGeneratedChoice
{
  public string Id { get; set; } = string.Empty;

  public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Forme JSON attendue de la réponse du LLM pour un chapitre
/// (voir <see cref="Prompts.PromptTemplateRegistry.ChapterSystemPrompt"/>). La conformité à la
/// nature du chapitre est vérifiée en couche Application.
/// </summary>
public sealed class RawGeneratedChapter
{
  /// <summary>Titre de l'histoire (chapitre 1 uniquement).</summary>
  public string? Title { get; set; }

  public string Text { get; set; } = string.Empty;

  public List<RawGeneratedChoice> Choices { get; set; } = [];

  public string UpdatedSummary { get; set; } = string.Empty;

  public List<string> KeyFacts { get; set; } = [];

  /// <summary>Description visuelle générique du chapitre, sans nom propre ni marque déposée.</summary>
  public string ImagePrompt { get; set; } = string.Empty;

  /// <summary>Plan d'ensemble de l'histoire (chapitre 1 uniquement).</summary>
  public List<string> StoryOutline { get; set; } = [];
}
