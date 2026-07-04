using FableFlow.Application.Themes.Dtos;
using FableFlow.Application.Themes.Queries.GetAvailableThemes;
using FableFlow.Application.Themes.Queries.GetStoryPremises;
using MediatR;

namespace FableFlow.Api.Endpoints;

/// <summary>Endpoints REST relatifs aux thèmes disponibles.</summary>
public static class ThemeEndpoints
{
  public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/themes", GetThemesAsync)
        .WithName("GetThemes")
        .WithTags("Themes")
        .WithSummary("Retourne les thèmes disponibles pour démarrer une aventure.")
        .Produces<IReadOnlyList<ThemeDto>>();

    app.MapGet("/api/themes/{themeId}/story-premises", GetStoryPremisesAsync)
        .WithName("GetStoryPremises")
        .WithTags("Themes")
        .WithSummary("Génère des propositions d'axe narratif pour un thème (rejouable via 'Refresh').")
        .Produces<IReadOnlyList<StoryPremiseDto>>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

    return app;
  }

  private static async Task<IResult> GetThemesAsync(
      ISender sender,
      CancellationToken cancellationToken)
  {
    var result = await sender.Send(new GetAvailableThemesQuery(), cancellationToken);
    return Results.Ok(result);
  }

  private static async Task<IResult> GetStoryPremisesAsync(
      string themeId,
      ISender sender,
      CancellationToken cancellationToken,
      int count = 3)
  {
    var result = await sender.Send(new GetStoryPremisesQuery(themeId, count), cancellationToken);
    return Results.Ok(result);
  }
}
