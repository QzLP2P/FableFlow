using FableFlow.Domain.Entities;

namespace FableFlow.Application.Adventures.Services;

/// <summary>
/// Écrit le chapitre réservé par une transition : génération, contrôle de forme, mémoire narrative
/// et planification de l'illustration. Partagé par le démarrage, l'avancée et les choix.
/// </summary>
public interface IChapterWriter
{
  /// <summary>
  /// Génère et attache le chapitre. En cas d'échec ou d'annulation, la réservation est libérée et
  /// l'histoire reste dans son état précédent (l'action peut être retentée).
  /// </summary>
  Task WriteAsync(
      AdventureSession session,
      ThemeDefinition theme,
      ChapterTransition transition,
      CancellationToken cancellationToken);
}
