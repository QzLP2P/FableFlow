namespace FableFlow.Application.Abstractions;

/// <summary>
/// Port de mise en file de travaux exécutés en arrière-plan, hors du cycle de vie de la requête
/// HTTP courante. Utilisé pour ne jamais faire attendre une réponse rapide (texte et choix d'une
/// scène) derrière un traitement plus lent (génération d'image) : voir <c>ISceneImageJobScheduler</c>.
/// </summary>
public interface IBackgroundJobQueue
{
  /// <summary>
  /// Met en file un travail à exécuter dès que possible en arrière-plan. Le jeton d'annulation fourni
  /// à l'exécution est lié à la durée de vie de l'application (pas à la requête HTTP d'origine, déjà
  /// terminée au moment de l'exécution). Toute exception levée par le travail est journalisée par le
  /// consommateur de la file sans jamais interrompre le traitement des travaux suivants.
  /// </summary>
  void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
}
