using FableFlow.Domain.Enums;

namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Un choix produit par le LLM, avec son impact narratif.</summary>
public sealed record GeneratedChoice(string Id, string Label, ChoiceOutcome Outcome);

/// <summary>
/// Scène produite par le service de génération narrative : texte, choix,
/// résumé mis à jour et faits clés à préserver pour la cohérence.
/// </summary>
public sealed record GeneratedScene(
    string Text,
    IReadOnlyList<GeneratedChoice> Choices,
    string UpdatedSummary,
    IReadOnlyList<string> KeyFacts);
