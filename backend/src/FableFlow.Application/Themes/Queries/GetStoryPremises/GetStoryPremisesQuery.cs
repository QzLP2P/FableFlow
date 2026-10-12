using FableFlow.Application.Common.Heroes;
using FableFlow.Application.Themes.Dtos;
using FableFlow.Domain.Enums;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

/// <summary>
/// Génère des idées de départ personnalisées (héros, âge, mode de lecture, longueur) et sans
/// divulgation de l'intrigue. Rejouable : chaque appel produit un nouveau lot, rien n'est persisté.
/// </summary>
public sealed record GetStoryPremisesQuery(
    string ThemeId,
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroInput Hero,
    int Count = 3) : IRequest<IReadOnlyList<StoryPremiseDto>>;
