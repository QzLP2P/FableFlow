namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Action proposée par le LLM lors d'un moment de choix (aucune n'est « bonne » ou « mauvaise »).</summary>
public sealed record GeneratedChoice(string Id, string Label);

/// <summary>
/// Chapitre produit par le service de génération narrative : texte, actions éventuelles,
/// mémoire narrative mise à jour et description visuelle pour l'illustration.
/// </summary>
/// <param name="Text">Texte du chapitre.</param>
/// <param name="Choices">Actions proposées ; attendues uniquement pour un moment de choix.</param>
/// <param name="UpdatedSummary">Résumé cumulatif mis à jour de l'histoire.</param>
/// <param name="KeyFacts">Faits clés à préserver pour la cohérence narrative.</param>
/// <param name="ImagePrompt">
/// Description visuelle générique du chapitre, sans nom propre ni marque déposée, destinée à la
/// génération d'image. La description stable du héros est ajoutée par <see cref="IPromptBuilder"/>.
/// </param>
/// <param name="StoryOutline">Plan d'ensemble, demandé uniquement à l'ouverture de l'histoire.</param>
/// <param name="Title">Titre proposé, demandé uniquement à l'ouverture si aucun titre n'a été choisi.</param>
public sealed record GeneratedChapter(
    string Text,
    IReadOnlyList<GeneratedChoice> Choices,
    string UpdatedSummary,
    IReadOnlyList<string> KeyFacts,
    string ImagePrompt,
    IReadOnlyList<string>? StoryOutline = null,
    string? Title = null);
