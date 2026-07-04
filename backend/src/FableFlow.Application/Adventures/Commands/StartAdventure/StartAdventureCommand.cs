using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

/// <summary>Démarre une nouvelle aventure et génère la scène initiale.</summary>
public sealed record StartAdventureCommand(string ThemeId, int SceneCount)
    : IRequest<AdventureDto>;
