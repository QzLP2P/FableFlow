namespace FableFlow.Domain.Enums;

/// <summary>État explicite de l'illustration d'un chapitre (jamais de succès fabriqué).</summary>
public enum IllustrationStatus
{
  /// <summary>Aucune illustration demandée (génération d'image désactivée).</summary>
  NotRequested = 0,

  /// <summary>Illustration en cours de génération en arrière-plan.</summary>
  Pending = 1,

  /// <summary>Illustration disponible.</summary>
  Ready = 2,

  /// <summary>La génération a échoué : le texte reste lisible sans image.</summary>
  Failed = 3
}
