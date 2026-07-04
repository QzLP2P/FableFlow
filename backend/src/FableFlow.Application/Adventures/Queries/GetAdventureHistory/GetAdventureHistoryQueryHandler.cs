using FableFlow.Application.Abstractions;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using MediatR;

namespace FableFlow.Application.Adventures.Queries.GetAdventureHistory;

public sealed class GetAdventureHistoryQueryHandler
    : IRequestHandler<GetAdventureHistoryQuery, AdventureHistoryDto>
{
  private readonly IAdventureRepository _repository;

  public GetAdventureHistoryQueryHandler(IAdventureRepository repository) =>
      _repository = repository;

  public async Task<AdventureHistoryDto> Handle(
      GetAdventureHistoryQuery request,
      CancellationToken cancellationToken)
  {
    var session = await _repository.GetAsync(request.AdventureId, cancellationToken)
        ?? throw new NotFoundException("Aventure", request.AdventureId);

    return session.ToHistoryDto();
  }
}
