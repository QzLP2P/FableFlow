using FableFlow.Application.Abstractions;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using MediatR;

namespace FableFlow.Application.Adventures.Queries.GetAdventureSession;

public sealed class GetAdventureSessionQueryHandler
    : IRequestHandler<GetAdventureSessionQuery, AdventureDto>
{
  private readonly IAdventureRepository _repository;
  private readonly IImageGenerationService _imageGeneration;

  public GetAdventureSessionQueryHandler(IAdventureRepository repository, IImageGenerationService imageGeneration)
  {
    _repository = repository;
    _imageGeneration = imageGeneration;
  }

  public async Task<AdventureDto> Handle(
      GetAdventureSessionQuery request,
      CancellationToken cancellationToken)
  {
    var session = await _repository.GetAsync(request.AdventureId, cancellationToken)
        ?? throw new NotFoundException("Aventure", request.AdventureId);

    return session.ToDto(_imageGeneration.IsEnabled);
  }
}
