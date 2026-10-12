using FableFlow.Domain.Enums;

namespace FableFlow.Application.Abstractions.Generation;

/// <summary>
/// Prompt structuré prêt à être envoyé au LLM. Produit par <see cref="IPromptBuilder"/>
/// à partir d'un gabarit versionné.
/// </summary>
public sealed record StoryPrompt(
    string SystemPrompt,
    string UserPrompt,
    string TemplateVersion,
    ChapterKind Kind,
    int ChapterNumber);

/// <summary>Prompt structuré pour la génération d'image d'un chapitre.</summary>
public sealed record StoryImagePrompt(string Prompt, string Style);
