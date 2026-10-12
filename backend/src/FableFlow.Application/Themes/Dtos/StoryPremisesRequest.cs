using FableFlow.Application.Common.Heroes;
using FableFlow.Domain.Enums;

namespace FableFlow.Application.Themes.Dtos;

/// <summary>
/// Corps de requête pour obtenir des idées de départ personnalisées. Envoyé en POST pour que les
/// données de l'enfant ne transitent jamais dans l'URL (journaux, historique, proxys).
/// </summary>
public sealed record StoryPremisesRequest(
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroInput Hero,
    int Count = 3);
