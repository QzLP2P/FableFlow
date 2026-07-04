using FableFlow.Application.Abstractions.Generation;

namespace FableFlow.Application.Abstractions;

/// <summary>Port vers le fournisseur de génération narrative (LLM).</summary>
public interface IStoryGenerationService
{
  /// <summary>Génère une scène à partir d'un prompt structuré.</summary>
  Task<GeneratedScene> GenerateSceneAsync(StoryPrompt prompt, CancellationToken cancellationToken);
}
