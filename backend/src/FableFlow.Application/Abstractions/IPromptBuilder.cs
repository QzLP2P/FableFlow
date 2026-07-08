using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Entities;

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
  /// Ne dépend que du thème (style, contraintes) : ne prend pas la session en paramètre, afin de
  /// pouvoir être appelée en toute sécurité depuis un travail planifié en arrière-plan (voir
  /// <c>ISceneImageJobScheduler</c>), longtemps après que la session ait pu évoluer.
  /// </summary>
  StoryImagePrompt BuildImagePrompt(ThemeDefinition theme, string genericSceneDescription);

  /// <summary>Construit le prompt de génération de plusieurs axes narratifs (premises) pour un thème.</summary>
  StoryPremisePrompt BuildPremisePrompt(ThemeDefinition theme, int count);
}
