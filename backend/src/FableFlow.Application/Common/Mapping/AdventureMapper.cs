using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Themes.Dtos;
using FableFlow.Domain.Entities;

namespace FableFlow.Application.Common.Mapping;

/// <summary>Convertit les entités du domaine en DTOs exposés par l'API.</summary>
public static class AdventureMapper
{
  public static ThemeDto ToDto(this ThemeDefinition theme) =>
      new(theme.Id, theme.DisplayName, theme.Audience.ToString(), theme.VocabularyLevel.ToString());

  public static SceneDto ToDto(this AdventureScene scene) =>
      new(
          scene.SceneNumber,
          scene.Text,
          scene.ImageUrl,
          [.. scene.Choices.Select(c => new ChoiceDto(c.Id, c.Label))]);

  public static AdventureDto ToDto(this AdventureSession session)
  {
    // La scène courante n'est proposée que si l'aventure est en cours.
    var currentScene = session.IsFinished ? null : session.CurrentScene?.ToDto();

    return new AdventureDto(
        session.Id,
        session.Status.ToString(),
        session.CurrentSceneNumber,
        session.TargetSceneCount,
        currentScene,
        session.Outcome?.Message,
        session.Outcome?.ImageUrl);
  }

  public static AdventureHistoryDto ToHistoryDto(this AdventureSession session) =>
      new(
          session.Id,
          session.Status.ToString(),
          [.. session.Scenes.Select(s => s.ToDto())]);
}
