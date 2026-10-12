using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions;

/// <summary>Construit des prompts structurés et versionnés pour la génération.</summary>
public interface IPromptBuilder
{
  /// <summary>Construit le prompt narratif du chapitre réservé par la transition.</summary>
  StoryPrompt BuildChapterPrompt(ChapterGenerationRequest request);

  /// <summary>
  /// Construit le prompt d'illustration d'un chapitre à partir d'une description visuelle générique
  /// (sans nom propre ni marque) et de la description stable du héros (âge, apparence). Ne dépend
  /// pas de la session vivante afin de pouvoir être appelée depuis un travail en arrière-plan.
  /// </summary>
  StoryImagePrompt BuildImagePrompt(ThemeDefinition theme, HeroProfile hero, string genericSceneDescription);

  /// <summary>Construit le prompt de génération d'idées de départ personnalisées.</summary>
  StoryPremisePrompt BuildPremisePrompt(StoryPremiseRequest request);
}
