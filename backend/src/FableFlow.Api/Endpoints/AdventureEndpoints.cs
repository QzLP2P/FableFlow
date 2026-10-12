using FableFlow.Application.Adventures.Commands.AdvanceChapter;
using FableFlow.Application.Adventures.Commands.MakeChoice;
using FableFlow.Application.Adventures.Commands.StartAdventure;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Adventures.Queries.GetAdventureHistory;
using FableFlow.Application.Adventures.Queries.GetAdventureSession;
using MediatR;

namespace FableFlow.Api.Endpoints;

/// <summary>Endpoints REST relatifs au cycle de vie d'une histoire.</summary>
public static class AdventureEndpoints
{
  public static IEndpointRouteBuilder MapAdventureEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/adventures").WithTags("Adventures");

    group.MapPost("/", StartAdventureAsync)
        .WithName("StartAdventure")
        .WithSummary("Démarre une histoire personnalisée et écrit son premier chapitre.")
        .Produces<AdventureDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status502BadGateway);

    group.MapGet("/{id:guid}", GetAdventureAsync)
        .WithName("GetAdventure")
        .WithSummary("Retourne l'état courant d'une histoire (chapitres écrits, illustrations).")
        .Produces<AdventureDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

    group.MapPost("/{id:guid}/advance", AdvanceChapterAsync)
        .WithName("AdvanceChapter")
        .WithSummary("Passe au chapitre suivant depuis un chapitre de récit.")
        .Produces<AdventureDto>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status502BadGateway);

    group.MapPost("/{id:guid}/choices", MakeChoiceAsync)
        .WithName("MakeChoice")
        .WithSummary("Retient l'action choisie lors d'un moment de choix et écrit le chapitre suivant.")
        .Produces<AdventureDto>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status502BadGateway);

    group.MapGet("/{id:guid}/history", GetAdventureHistoryAsync)
        .WithName("GetAdventureHistory")
        .WithSummary("Retourne l'histoire telle que lue, avec les actions choisies et la conclusion.")
        .Produces<AdventureHistoryDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

    return app;
  }

  private static async Task<IResult> StartAdventureAsync(
      StartAdventureRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var command = new StartAdventureCommand(
        request.ThemeId,
        request.Length,
        request.ReaderMode,
        request.Hero,
        request.Premise,
        request.PremiseTitle);
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

  private static async Task<IResult> AdvanceChapterAsync(
      Guid id,
      AdvanceChapterRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var result = await sender.Send(new AdvanceChapterCommand(id, request.FromChapterNumber), cancellationToken);
    return Results.Ok(result);
  }

  private static async Task<IResult> MakeChoiceAsync(
      Guid id,
      MakeChoiceRequest request,
      ISender sender,
      CancellationToken cancellationToken)
  {
    var command = new MakeChoiceCommand(id, request.ChapterNumber, request.ChoiceId);
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
