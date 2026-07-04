using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>Issue finale d'une aventure terminée.</summary>
public sealed class AdventureOutcome
{
  private AdventureOutcome(SessionStatus status, string message, string? imageUrl)
  {
    Status = status;
    Message = message;
    ImageUrl = imageUrl;
  }

  /// <summary>Statut terminal (<see cref="SessionStatus.Won"/>, <see cref="SessionStatus.Lost"/> ou <see cref="SessionStatus.Completed"/>).</summary>
  public SessionStatus Status { get; }

  /// <summary>Message narratif de conclusion.</summary>
  public string Message { get; }

  /// <summary>Illustration de la scène finale, le cas échéant (génération non garantie).</summary>
  public string? ImageUrl { get; }

  public static AdventureOutcome Won(string message, string? imageUrl = null) => new(SessionStatus.Won, message, imageUrl);

  public static AdventureOutcome Lost(string message, string? imageUrl = null) => new(SessionStatus.Lost, message, imageUrl);

  public static AdventureOutcome Completed(string message, string? imageUrl = null) => new(SessionStatus.Completed, message, imageUrl);
}
