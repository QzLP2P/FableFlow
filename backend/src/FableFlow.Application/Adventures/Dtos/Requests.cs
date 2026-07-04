namespace FableFlow.Application.Adventures.Dtos;

/// <summary>Corps de requête pour démarrer une aventure.</summary>
public sealed record StartAdventureRequest(string ThemeId, int SceneCount);

/// <summary>Corps de requête pour appliquer un choix utilisateur.</summary>
public sealed record MakeChoiceRequest(string ChoiceId);
