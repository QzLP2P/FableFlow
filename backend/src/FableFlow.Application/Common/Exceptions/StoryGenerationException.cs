namespace FableFlow.Application.Common.Exceptions;

/// <summary>
/// La génération narrative n'a pas produit de contenu exploitable (fournisseur indisponible ou
/// réponse invalide après nouvelle tentative). Le message est destiné à l'utilisateur et ne contient
/// aucun détail interne ; l'état de l'histoire est conservé et l'action peut être retentée.
/// </summary>
public sealed class StoryGenerationException : Exception
{
  public StoryGenerationException(string message)
      : base(message)
  {
  }

  public StoryGenerationException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}
