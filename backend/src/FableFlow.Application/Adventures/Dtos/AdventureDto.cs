namespace FableFlow.Application.Adventures.Dtos;

/// <summary>Choix présenté à l'utilisateur (sans exposer son impact narratif interne).</summary>
public sealed record ChoiceDto(string Id, string Label);

/// <summary>Scène telle qu'exposée au client.</summary>
public sealed record SceneDto(
    int SceneNumber,
    string Text,
    string? ImageUrl,
    IReadOnlyList<ChoiceDto> Choices);

/// <summary>État courant d'une aventure retourné par l'API.</summary>
public sealed record AdventureDto(
    Guid AdventureId,
    string Status,
    int CurrentSceneNumber,
    int TargetSceneCount,
    SceneDto? CurrentScene,
    string? OutcomeMessage);

/// <summary>Historique des scènes jouées.</summary>
public sealed record AdventureHistoryDto(
    Guid AdventureId,
    string Status,
    IReadOnlyList<SceneDto> Scenes);
