using FableFlow.Domain.Enums;

namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Un choix produit par le LLM, avec son impact narratif.</summary>
public sealed record GeneratedChoice(string Id, string Label, ChoiceOutcome Outcome);

/// <summary>
/// Scène produite par le service de génération narrative : texte, choix,
/// résumé mis à jour et faits clés à préserver pour la cohérence.
/// </summary>
/// <param name="Text">Texte narratif de la scène (peut nommer librement des personnages/univers du thème).</param>
/// <param name="Choices">Choix proposés à l'utilisateur.</param>
/// <param name="UpdatedSummary">Résumé cumulatif mis à jour de l'histoire.</param>
/// <param name="KeyFacts">Faits clés à préserver pour la cohérence narrative.</param>
/// <param name="ImagePrompt">
/// Description visuelle générique de la scène, volontairement dépourvue de noms propres ou de
/// marques déposées (personnages, franchises), destinée à la génération d'image. Voir
/// <see cref="Prompts.PromptTemplateRegistry.SceneSystemPrompt"/> pour la consigne donnée au LLM.
/// </param>
/// <param name="StoryOutline">
/// Plan d'ensemble de l'aventure (quelques grandes étapes/actes), demandé UNIQUEMENT pour une scène
/// de type <see cref="SceneKind.Initial"/> ; <c>null</c> ou vide pour une continuation/fin (le plan
/// est déjà fixé sur la session, voir <c>AdventureSession.StoryOutline</c>).
/// </param>
public sealed record GeneratedScene(
    string Text,
    IReadOnlyList<GeneratedChoice> Choices,
    string UpdatedSummary,
    IReadOnlyList<string> KeyFacts,
    string ImagePrompt,
    IReadOnlyList<string>? StoryOutline = null);
