using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

/// <summary>Démarre une nouvelle aventure et génère la scène initiale.</summary>
/// <param name="ThemeId">Identifiant du thème choisi.</param>
/// <param name="SceneCount">Nombre de scènes cible.</param>
/// <param name="NarrativePremise">
/// Axe narratif choisi par l'utilisateur parmi les propositions de <c>GetStoryPremisesQuery</c>
/// (optionnel). Ancre toute la génération narrative de l'aventure sur cette direction.
/// </param>
public sealed record StartAdventureCommand(string ThemeId, int SceneCount, string? NarrativePremise = null)
    : IRequest<AdventureDto>;
