using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

/// <summary>Applique un choix utilisateur et génère la suite de l'aventure.</summary>
public sealed record MakeChoiceCommand(Guid AdventureId, string ChoiceId)
    : IRequest<AdventureDto>;
