using FableFlow.Application.Abstractions;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

public sealed class MakeChoiceCommandHandler
    : IRequestHandler<MakeChoiceCommand, AdventureDto>
{
  private readonly IAdventureRepository _repository;
  private readonly IThemePolicyProvider _themeProvider;
  private readonly IChapterWriter _chapterWriter;
  private readonly IImageGenerationService _imageGeneration;

  public MakeChoiceCommandHandler(
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
      MakeChoiceCommand request,
      CancellationToken cancellationToken)
  {
    var session = await _repository.GetAsync(request.AdventureId, cancellationToken)
        ?? throw new NotFoundException("Histoire", request.AdventureId);

    var theme = await _themeProvider.FindThemeAsync(session.ThemeId, cancellationToken)
        ?? throw new NotFoundException("Univers", session.ThemeId);

    var transition = session.BeginChoice(request.ChapterNumber, request.ChoiceId);
    await _chapterWriter.WriteAsync(session, theme, transition, cancellationToken);
    await _repository.SaveAsync(session, cancellationToken);

    return session.ToDto(_imageGeneration.IsEnabled);
  }
}
