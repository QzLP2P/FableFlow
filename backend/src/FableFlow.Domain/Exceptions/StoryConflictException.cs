namespace FableFlow.Domain.Exceptions;

/// <summary>
/// Action de lecture incompatible avec l'état courant de l'histoire (mauvais type d'action, action
/// déjà appliquée, chapitre en cours de génération, histoire terminée...). Distincte de
/// <see cref="DomainException"/> car elle signale un conflit d'état et non une donnée invalide.
/// </summary>
public sealed class StoryConflictException : Exception
{
  public StoryConflictException(string message) : base(message)
  {
  }
}
