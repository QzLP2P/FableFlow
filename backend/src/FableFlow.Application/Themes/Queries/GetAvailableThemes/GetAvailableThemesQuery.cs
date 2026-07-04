using FableFlow.Application.Themes.Dtos;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetAvailableThemes;

/// <summary>Retourne les thèmes disponibles pour démarrer une aventure.</summary>
public sealed record GetAvailableThemesQuery : IRequest<IReadOnlyList<ThemeDto>>;
