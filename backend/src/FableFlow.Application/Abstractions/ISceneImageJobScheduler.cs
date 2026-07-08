using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions;

/// <summary>
/// Planifie en arrière-plan la génération d'illustration d'une scène ou de l'issue d'une aventure,
/// afin que la réponse HTTP contenant le texte et les choix (rapide) ne soit jamais retardée par la
/// génération d'image (nettement plus lente). L'image est attachée a posteriori dès qu'elle est
/// prête ; le client la récupère via un nouvel appel à <c>GET /api/adventures/{id}</c> (polling).
/// </summary>
public interface ISceneImageJobScheduler
{
  /// <summary>
  /// Planifie l'illustration d'une scène identifiée par son numéro. Le numéro (plutôt que "la scène
  /// courante") est nécessaire car l'utilisateur peut avoir fait progresser l'aventure vers une
  /// nouvelle scène avant que cette image ne soit prête.
  /// </summary>
  void ScheduleForScene(Guid adventureId, int sceneNumber, ThemeDefinition theme, string genericSceneDescription);

  /// <summary>Planifie l'illustration de l'issue finale d'une aventure terminée.</summary>
  void ScheduleForOutcome(Guid adventureId, ThemeDefinition theme, string genericSceneDescription);
}
