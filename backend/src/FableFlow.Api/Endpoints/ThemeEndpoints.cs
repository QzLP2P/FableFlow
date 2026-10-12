using FableFlow.Application.Themes.Dtos;
using FableFlow.Application.Themes.Queries.GetAvailableThemes;
using FableFlow.Application.Themes.Queries.GetStoryPremises;
using MediatR;

namespace FableFlow.Api.Endpoints;

/// <summary>Endpoints REST relatifs aux univers disponibles.</summary>
public static class ThemeEndpoints
{
  public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/themes", GetThemesAsync)
        .WithName("GetThemes")
        .WithTags("Themes")
        .WithSummary("Retourne les univers disponibles pour créer une histoire.")
        .Produces<IReadOnlyList<ThemeDto>>();

    // POST car la génération dépend du héros (données personnelles à ne pas exposer dans l'URL ni
    // dans les journaux d'accès) et n'est ni sûre ni idempotente (chaque appel propose d'autres idées).
    app.MapPost("/api/themes/{themeId}/story-premises", GetStoryPremisesAsync)
        .WithName("GetStoryPremises")
        .WithTags("Themes")
        .WithSummary("Propose des idées de départ personnalisées pour un univers et un héros.")
        .Produces<IReadOnlyList<StoryPremiseDto>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status502BadGateway);

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
      StoryPremisesRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var query = new GetStoryPremisesQuery(themeId, request.Length, request.ReaderMode, request.Hero, request.Count);
    var result = await sender.Send(query, cancellationToken);
    return Results.Ok(result);
  }
}
