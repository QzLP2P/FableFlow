using FableFlow.Application.Themes.Dtos;
using FableFlow.Application.Themes.Queries.GetAvailableThemes;
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

    return app;
  }

  private static async Task<IResult> GetThemesAsync(
      ISender sender,
      CancellationToken cancellationToken)
  {
    var result = await sender.Send(new GetAvailableThemesQuery(), cancellationToken);
    return Results.Ok(result);
  }
}
