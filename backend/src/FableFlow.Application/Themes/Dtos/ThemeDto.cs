namespace FableFlow.Application.Themes.Dtos;

/// <summary>Thème disponible exposé par l'API.</summary>
public sealed record ThemeDto(
    string Id,
    string DisplayName,
    string Audience,
    string VocabularyLevel);
