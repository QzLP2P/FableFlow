using FableFlow.Application.Abstractions.Generation;

namespace FableFlow.Application.Abstractions;

/// <summary>Port vers le fournisseur de génération narrative (LLM).</summary>
public interface IStoryGenerationService
{
  /// <summary>Génère un chapitre à partir d'un prompt structuré.</summary>
  Task<GeneratedChapter> GenerateChapterAsync(StoryPrompt prompt, CancellationToken cancellationToken);

  /// <summary>Génère plusieurs idées de départ personnalisées.</summary>
  Task<IReadOnlyList<GeneratedPremise>> GeneratePremisesAsync(StoryPremisePrompt prompt, CancellationToken cancellationToken);
}
