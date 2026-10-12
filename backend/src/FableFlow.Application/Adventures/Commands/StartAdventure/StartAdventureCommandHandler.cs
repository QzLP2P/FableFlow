using FableFlow.Application.Abstractions;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using FableFlow.Domain.Entities;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

public sealed class StartAdventureCommandHandler
    : IRequestHandler<StartAdventureCommand, AdventureDto>
{
  private readonly IAdventureRepository _repository;
  private readonly IThemePolicyProvider _themeProvider;
  private readonly IChapterWriter _chapterWriter;
  private readonly IImageGenerationService _imageGeneration;

  public StartAdventureCommandHandler(
      IAdventureRepository repository,
      IThemePolicyProvider themeProvider,
      IChapterWriter chapterWriter,
      IImageGenerationService imageGeneration)
  {
    _repository = repository;
    _themeProvider = themeProvider;
    _chapterWriter = chapterWriter;
    _imageGeneration = imageGeneration;
  }

  public async Task<AdventureDto> Handle(
      StartAdventureCommand request,
      CancellationToken cancellationToken)
  {
    var theme = await _themeProvider.FindThemeAsync(request.ThemeId, cancellationToken)
        ?? throw new NotFoundException("Univers", request.ThemeId);

    var session = AdventureSession.Start(
        Guid.NewGuid(),
        theme.Id,
        request.Hero.ToProfile(),
        request.ReaderMode,
        request.Length,
        request.Premise,
        request.PremiseTitle);

    // L'histoire n'est enregistrée qu'une fois son premier chapitre écrit : un échec de génération
    // ne laisse aucune histoire vide derrière lui.
    await _chapterWriter.WriteAsync(session, theme, session.BeginOpening(), cancellationToken);
    await _repository.SaveAsync(session, cancellationToken);

    return session.ToDto(_imageGeneration.IsEnabled);
  }
}
