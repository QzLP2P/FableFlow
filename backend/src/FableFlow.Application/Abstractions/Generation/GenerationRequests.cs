using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;

namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Contexte complet nécessaire à la construction du prompt d'un chapitre.</summary>
/// <param name="Theme">Univers et garde-fous de contenu.</param>
/// <param name="Session">Histoire en cours (héros, mémoire narrative, chapitres écrits).</param>
/// <param name="Transition">Chapitre réservé à écrire (numéro, nature, action choisie éventuelle).</param>
public sealed record ChapterGenerationRequest(
    ThemeDefinition Theme,
    AdventureSession Session,
    ChapterTransition Transition);

/// <summary>Contexte de génération des idées de départ, personnalisées pour le héros.</summary>
public sealed record StoryPremiseRequest(
    ThemeDefinition Theme,
    HeroProfile Hero,
    ReaderMode ReaderMode,
    StoryLength Length,
    int Count);
