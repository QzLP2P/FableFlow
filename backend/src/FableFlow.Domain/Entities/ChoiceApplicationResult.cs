using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>Résultat de l'application d'un choix sur une session.</summary>
public sealed record ChoiceApplicationResult(
    SessionStatus Status,
    ChoiceOutcome ChoiceOutcome,
    bool RequiresNextScene)
{
    public bool IsTerminal => Status != SessionStatus.InProgress;
}
