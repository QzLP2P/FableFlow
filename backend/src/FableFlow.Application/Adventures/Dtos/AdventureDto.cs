using FableFlow.Domain.Enums;

namespace FableFlow.Application.Adventures.Dtos;

/// <summary>Action proposée au lecteur.</summary>
public sealed record ChoiceDto(string Id, string Label);

/// <summary>État de l'illustration d'un chapitre ; l'URL n'est renseignée que lorsqu'elle est prête.</summary>
public sealed record IllustrationDto(IllustrationStatus Status, string? Url);

/// <summary>Chapitre tel qu'exposé au client.</summary>
/// <param name="SelectedChoiceId">Action retenue (moment de choix déjà dépassé), sinon <c>null</c>.</param>
public sealed record ChapterDto(
    int Number,
    ChapterKind Kind,
    string Text,
    IReadOnlyList<ChoiceDto> Choices,
    string? SelectedChoiceId,
    IllustrationDto Illustration);

/// <summary>Instantané du héros de l'histoire.</summary>
public sealed record HeroDto(
    string Name,
    int Age,
    IReadOnlyList<string> Companions,
    string? Interests,
    string? Personality,
    string? Appearance);

/// <summary>
/// Histoire complète retournée par l'API après chaque action : configuration, progression et tous
/// les chapitres déjà écrits (le client choisit le chapitre affiché ; seul le dernier est actionnable).
/// </summary>
/// <param name="CurrentChapterNumber">Chapitre le plus avancé (frontière de lecture).</param>
/// <param name="DecisionChapters">Numéros des chapitres proposant un choix (calendrier fixe).</param>
/// <param name="IsGenerating">Indique qu'un chapitre est en cours d'écriture.</param>
public sealed record AdventureDto(
    Guid AdventureId,
    SessionStatus Status,
    string? Title,
    string ThemeId,
    string Premise,
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroDto Hero,
    int TotalChapters,
    IReadOnlyList<int> DecisionChapters,
    int CurrentChapterNumber,
    bool IsGenerating,
    bool ImageGenerationEnabled,
    IReadOnlyList<ChapterDto> Chapters);

/// <summary>Chapitre du livre relu : le chemin suivi est explicite (libellé de l'action retenue).</summary>
public sealed record HistoryChapterDto(
    int Number,
    ChapterKind Kind,
    string Text,
    string? SelectedChoiceLabel,
    IllustrationDto Illustration);

/// <summary>Livre de l'histoire : métadonnées, chapitres, chemin suivi et conclusion.</summary>
/// <param name="Conclusion">Texte de la conclusion, une fois l'histoire terminée.</param>
public sealed record AdventureHistoryDto(
    Guid AdventureId,
    SessionStatus Status,
    string? Title,
    string ThemeId,
    string Premise,
    StoryLength Length,
    ReaderMode ReaderMode,
    HeroDto Hero,
    int TotalChapters,
    IReadOnlyList<HistoryChapterDto> Chapters,
    string? Conclusion);
