using FableFlow.Application.Common.Heroes;
using FableFlow.Domain.Enums;

namespace FableFlow.Application.Adventures.Dtos;

/// <summary>Corps de requête pour démarrer une histoire personnalisée.</summary>
/// <param name="ThemeId">Univers choisi.</param>
/// <param name="Length">Longueur (courte ou plus longue).</param>
/// <param name="ReaderMode">Qui lit : un adulte à voix haute ou l'enfant.</param>
/// <param name="Hero">Héros de l'histoire (instantané).</param>
/// <param name="Premise">Idée de départ choisie ou saisie.</param>
/// <param name="PremiseTitle">Titre de l'idée proposée, absent pour une idée personnelle.</param>
public sealed record StartAdventureRequest(
    string ThemeId,
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroInput Hero,
    string Premise,
    string? PremiseTitle = null);

/// <summary>Corps de requête pour passer au chapitre suivant depuis un chapitre de récit.</summary>
/// <param name="FromChapterNumber">Chapitre courant vu par le lecteur (détecte les doublons).</param>
public sealed record AdvanceChapterRequest(int FromChapterNumber);

/// <summary>Corps de requête pour retenir une action lors d'un moment de choix.</summary>
public sealed record MakeChoiceRequest(int ChapterNumber, string ChoiceId);
