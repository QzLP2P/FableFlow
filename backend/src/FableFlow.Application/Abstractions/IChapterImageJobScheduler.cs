using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions;

/// <summary>
/// Planifie en arrière-plan l'illustration d'un chapitre, afin que la réponse contenant le texte
/// (rapide) ne soit jamais retardée par la génération d'image (plus lente). L'état de l'illustration
/// (en attente, prête, indisponible) est porté par le chapitre ; le client le relit via
/// <c>GET /api/adventures/{id}</c> tant qu'une illustration est en attente.
/// </summary>
public interface IChapterImageJobScheduler
{
  /// <summary>
  /// Marque l'illustration du chapitre comme en attente puis planifie sa génération. Sans effet si
  /// la génération d'images est désactivée ; marque l'illustration indisponible si aucune
  /// description visuelle n'est fournie.
  /// </summary>
  void ScheduleForChapter(AdventureSession session, int chapterNumber, ThemeDefinition theme, string? genericSceneDescription);
}
