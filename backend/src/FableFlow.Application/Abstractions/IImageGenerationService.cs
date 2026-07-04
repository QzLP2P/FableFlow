using FableFlow.Application.Abstractions.Generation;

namespace FableFlow.Application.Abstractions;

/// <summary>Port vers le fournisseur de génération d'images.</summary>
public interface IImageGenerationService
{
    /// <summary>Indique si la génération d'images est active.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Génère une image pour une scène et retourne son URL, ou <c>null</c>
    /// si la génération est désactivée ou a échoué.
    /// </summary>
    Task<string?> GenerateImageAsync(StoryImagePrompt prompt, CancellationToken cancellationToken);
}
