namespace FableFlow.Domain.Enums;

/// <summary>Nature d'un chapitre, fixée par le calendrier de l'histoire (jamais par le modèle).</summary>
public enum ChapterKind
{
  /// <summary>Chapitre de récit : le lecteur passe simplement au chapitre suivant.</summary>
  Narrative = 0,

  /// <summary>Moment de choix : 2 à 3 actions non notées sont proposées.</summary>
  Decision = 1,

  /// <summary>Conclusion narrative : dernier chapitre, sans choix.</summary>
  Ending = 2
}
