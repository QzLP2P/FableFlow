using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Queries.GetAdventureHistory;

/// <summary>Retourne les scènes déjà jouées d'une aventure.</summary>
public sealed record GetAdventureHistoryQuery(Guid AdventureId) : IRequest<AdventureHistoryDto>;
