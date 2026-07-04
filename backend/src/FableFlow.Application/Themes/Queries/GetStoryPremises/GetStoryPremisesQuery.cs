using FableFlow.Application.Themes.Dtos;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

/// <summary>
/// Génère plusieurs propositions d'axe narratif pour un thème donné, afin que l'utilisateur en
/// choisisse une avant de démarrer l'aventure. Rejouable ("Refresh"/"More") pour obtenir d'autres
/// propositions : chaque appel régénère un nouveau lot, aucun état n'est persisté.
/// </summary>
public sealed record GetStoryPremisesQuery(string ThemeId, int Count = 3)
    : IRequest<IReadOnlyList<StoryPremiseDto>>;
