using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Common.Heroes;
using FableFlow.Domain.Enums;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

/// <summary>
/// Démarre une histoire personnalisée à partir d'un instantané du héros, du mode de lecture, de la
/// longueur et de l'idée de départ, puis écrit le premier chapitre.
/// </summary>
public sealed record StartAdventureCommand(
    string ThemeId,
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroInput Hero,
    string Premise,
    string? PremiseTitle = null) : IRequest<AdventureDto>;
