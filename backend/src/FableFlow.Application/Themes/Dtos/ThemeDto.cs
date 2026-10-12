namespace FableFlow.Application.Themes.Dtos;

/// <summary>Univers disponible exposé par l'API.</summary>
/// <param name="Description">Courte présentation, sans divulgation d'intrigue.</param>
public sealed record ThemeDto(
    string Id,
    string DisplayName,
    string Description);
