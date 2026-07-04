using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>Issue finale d'une aventure terminée.</summary>
public sealed class AdventureOutcome
{
  private AdventureOutcome(SessionStatus status, string message)
  {
    Status = status;
    Message = message;
  }

  /// <summary>Statut terminal (<see cref="SessionStatus.Won"/>, <see cref="SessionStatus.Lost"/> ou <see cref="SessionStatus.Completed"/>).</summary>
  public SessionStatus Status { get; }

  /// <summary>Message narratif de conclusion.</summary>
  public string Message { get; }

  public static AdventureOutcome Won(string message) => new(SessionStatus.Won, message);

  public static AdventureOutcome Lost(string message) => new(SessionStatus.Lost, message);

  public static AdventureOutcome Completed(string message) => new(SessionStatus.Completed, message);
}
