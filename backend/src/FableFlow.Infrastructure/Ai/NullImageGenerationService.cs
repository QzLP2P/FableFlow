using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Implémentation neutre utilisée lorsque la génération d'images est désactivée
/// (feature flag <c>Features:ImageGeneration</c>). Évite de coupler l'appelant à un flag global.
/// </summary>
public sealed class NullImageGenerationService : IImageGenerationService
{
  public bool IsEnabled => false;

  public Task<string?> GenerateImageAsync(StoryImagePrompt prompt, CancellationToken cancellationToken) =>
      Task.FromResult<string?>(null);
}
