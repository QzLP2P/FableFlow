using FableFlow.Domain.Enums;

namespace FableFlow.Domain.Entities;

/// <summary>
/// Réservation d'une transition vers le chapitre suivant, émise par <see cref="AdventureSession"/>
/// avant la génération (lente et faillible) du chapitre. Tant qu'elle est en cours, toute autre
/// action est refusée ; si la génération échoue, la réservation est abandonnée et l'état lisible
/// précédent reste intact, sans choix enregistré ni chapitre sauté.
/// </summary>
public sealed class ChapterTransition
{
  internal ChapterTransition(int fromChapterNumber, int nextChapterNumber, ChapterKind nextKind, ChapterChoice? selectedChoice)
  {
    Token = Guid.NewGuid();
    FromChapterNumber = fromChapterNumber;
    NextChapterNumber = nextChapterNumber;
    NextKind = nextKind;
    SelectedChoice = selectedChoice;
  }

  public Guid Token { get; }

  /// <summary>Chapitre courant au moment de l'action (0 pour l'ouverture de l'histoire).</summary>
  public int FromChapterNumber { get; }

  public int NextChapterNumber { get; }

  /// <summary>Nature attendue du chapitre à générer, imposée par le calendrier.</summary>
  public ChapterKind NextKind { get; }

  /// <summary>Action choisie par le lecteur, ou <c>null</c> pour un simple passage au chapitre suivant.</summary>
  public ChapterChoice? SelectedChoice { get; }
}
