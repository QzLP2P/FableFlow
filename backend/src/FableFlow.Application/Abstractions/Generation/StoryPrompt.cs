namespace FableFlow.Application.Abstractions.Generation;

/// <summary>
/// Prompt structuré prêt à être envoyé au LLM. Produit par <see cref="IPromptBuilder"/>
/// à partir d'un gabarit versionné.
/// </summary>
public sealed record StoryPrompt(
    string SystemPrompt,
    string UserPrompt,
    string TemplateVersion,
    SceneKind Kind,
    int SceneNumber);

/// <summary>Prompt structuré pour la génération d'image d'une scène.</summary>
public sealed record StoryImagePrompt(string Prompt, string Style);
