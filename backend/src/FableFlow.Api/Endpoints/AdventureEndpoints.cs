using FableFlow.Application.Adventures.Commands.MakeChoice;
using FableFlow.Application.Adventures.Commands.StartAdventure;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Adventures.Queries.GetAdventureHistory;
using FableFlow.Application.Adventures.Queries.GetAdventureSession;
using MediatR;

namespace FableFlow.Api.Endpoints;

/// <summary>Endpoints REST relatifs au cycle de vie d'une aventure.</summary>
public static class AdventureEndpoints
{
  public static IEndpointRouteBuilder MapAdventureEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/adventures").WithTags("Adventures");

    group.MapPost("/", StartAdventureAsync)
        .WithName("StartAdventure")
        .WithSummary("Démarre une nouvelle aventure et génère la scène initiale.")
        .Produces<AdventureDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

    group.MapGet("/{id:guid}", GetAdventureAsync)
        .WithName("GetAdventure")
        .WithSummary("Retourne l'état courant d'une aventure.")
        .Produces<AdventureDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

    group.MapPost("/{id:guid}/choices", MakeChoiceAsync)
        .WithName("MakeChoice")
        .WithSummary("Applique un choix et génère la scène suivante.")
        .Produces<AdventureDto>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

    group.MapGet("/{id:guid}/history", GetAdventureHistoryAsync)
        .WithName("GetAdventureHistory")
        .WithSummary("Retourne les scènes déjà jouées d'une aventure.")
        .Produces<AdventureHistoryDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

    return app;
  }

  private static async Task<IResult> StartAdventureAsync(
      StartAdventureRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var command = new StartAdventureCommand(request.ThemeId, request.SceneCount);
    var result = await sender.Send(command, cancellationToken);
    return Results.CreatedAtRoute("GetAdventure", new { id = result.AdventureId }, result);
  }

  private static async Task<IResult> GetAdventureAsync(
      Guid id,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var result = await sender.Send(new GetAdventureSessionQuery(id), cancellationToken);
    return Results.Ok(result);
  }

  private static async Task<IResult> MakeChoiceAsync(
      Guid id,
      MakeChoiceRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var command = new MakeChoiceCommand(id, request.ChoiceId);
    var result = await sender.Send(command, cancellationToken);
    return Results.Ok(result);
  }

  private static async Task<IResult> GetAdventureHistoryAsync(
      Guid id,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var result = await sender.Send(new GetAdventureHistoryQuery(id), cancellationToken);
    return Results.Ok(result);
  }
}
