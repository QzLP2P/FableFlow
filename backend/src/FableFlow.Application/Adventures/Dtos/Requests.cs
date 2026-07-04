namespace FableFlow.Application.Adventures.Dtos;

/// <summary>Corps de requête pour démarrer une aventure.</summary>
/// <param name="ThemeId">Identifiant du thème choisi.</param>
/// <param name="SceneCount">Nombre de scènes cible.</param>
/// <param name="NarrativePremise">Axe narratif choisi parmi les propositions (optionnel).</param>
public sealed record StartAdventureRequest(string ThemeId, int SceneCount, string? NarrativePremise = null);

/// <summary>Corps de requête pour appliquer un choix utilisateur.</summary>
public sealed record MakeChoiceRequest(string ChoiceId);
