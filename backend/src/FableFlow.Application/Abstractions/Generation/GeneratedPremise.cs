namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Une proposition d'axe narratif (pitch court) pour démarrer une aventure.</summary>
public sealed record GeneratedPremise(string Title, string Hook);

/// <summary>Prompt structuré pour la génération de plusieurs axes narratifs (premises).</summary>
public sealed record StoryPremisePrompt(
    string SystemPrompt,
    string UserPrompt,
    string TemplateVersion,
    int Count);
