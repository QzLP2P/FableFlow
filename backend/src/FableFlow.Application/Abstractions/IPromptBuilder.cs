using FableFlow.Application.Abstractions.Generation;

namespace FableFlow.Application.Abstractions;

/// <summary>Construit des prompts structurés et versionnés pour la génération.</summary>
public interface IPromptBuilder
{
  /// <summary>Construit le prompt narratif d'une scène.</summary>
  StoryPrompt BuildScenePrompt(SceneGenerationRequest request);

  /// <summary>
  /// Construit le prompt d'illustration d'une scène à partir d'une description visuelle
  /// générique (voir <see cref="GeneratedScene.ImagePrompt"/>), volontairement dépourvue de noms
  /// propres ou de marques déposées pour rester conforme aux filtres de contenu des fournisseurs d'image.
  /// </summary>
  StoryImagePrompt BuildImagePrompt(SceneGenerationRequest request, string genericSceneDescription);
}
