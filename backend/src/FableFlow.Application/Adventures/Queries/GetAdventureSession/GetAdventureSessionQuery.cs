using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Queries.GetAdventureSession;

/// <summary>Retourne l'état courant d'une aventure.</summary>
public sealed record GetAdventureSessionQuery(Guid AdventureId) : IRequest<AdventureDto>;
