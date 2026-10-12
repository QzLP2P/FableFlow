using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Themes.Dtos;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;

namespace FableFlow.Application.Common.Mapping;

/// <summary>Convertit les entités du domaine en DTOs exposés par l'API.</summary>
public static class AdventureMapper
{
  public static ThemeDto ToDto(this ThemeDefinition theme) =>
      new(theme.Id, theme.DisplayName, theme.Description);

  public static HeroDto ToDto(this HeroProfile hero) =>
      new(hero.Name, hero.Age, hero.Companions, hero.Interests, hero.Personality, hero.Appearance);

  public static AdventureDto ToDto(this AdventureSession session, bool imageGenerationEnabled) =>
      new(
          session.Id,
          session.Status,
          session.Title,
          session.ThemeId,
          session.Premise,
          session.Length,
          session.ReaderMode,
          session.Hero.ToDto(),
          session.TotalChapters,
          session.Schedule.DecisionChapters,
          session.CurrentChapterNumber,
          session.IsGenerating,
          imageGenerationEnabled,
          [.. session.Chapters.Select(ToDto)]);

  public static AdventureHistoryDto ToHistoryDto(this AdventureSession session)
  {
    var chapters = session.Chapters;
    var ending = chapters.FirstOrDefault(c => c.Kind == ChapterKind.Ending);

    return new AdventureHistoryDto(
        session.Id,
        session.Status,
        session.Title,
        session.ThemeId,
        session.Premise,
        session.Length,
        session.ReaderMode,
        session.Hero.ToDto(),
        session.TotalChapters,
        [.. chapters.Select(c => new HistoryChapterDto(
            c.Number,
            c.Kind,
            c.Text,
            c.SelectedChoice?.Label,
            c.ToIllustrationDto()))],
        ending?.Text);
  }

  private static ChapterDto ToDto(AdventureChapter chapter) =>
      new(
          chapter.Number,
          chapter.Kind,
          chapter.Text,
          [.. chapter.Choices.Select(c => new ChoiceDto(c.Id, c.Label))],
          chapter.SelectedChoiceId,
          chapter.ToIllustrationDto());

  private static IllustrationDto ToIllustrationDto(this AdventureChapter chapter) =>
      new(
          chapter.IllustrationStatus,
          chapter.IllustrationStatus == IllustrationStatus.Ready ? chapter.ImageUrl : null);
}
