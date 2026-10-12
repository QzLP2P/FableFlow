using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Themes.Dtos;
using FableFlow.Domain.Entities;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

public sealed class GetStoryPremisesQueryHandler
    : IRequestHandler<GetStoryPremisesQuery, IReadOnlyList<StoryPremiseDto>>
{
  private const string _generationFailedMessage =
      "Les idées d'histoire n'ont pas pu être proposées pour le moment. Réessayez dans un instant.";

  private readonly IThemePolicyProvider _themeProvider;
  private readonly IPromptBuilder _promptBuilder;
  private readonly IStoryGenerationService _storyGeneration;

  public GetStoryPremisesQueryHandler(
      IThemePolicyProvider themeProvider,
      IPromptBuilder promptBuilder,
      IStoryGenerationService storyGeneration)
  {
    _themeProvider = themeProvider;
    _promptBuilder = promptBuilder;
    _storyGeneration = storyGeneration;
  }

  public async Task<IReadOnlyList<StoryPremiseDto>> Handle(
      GetStoryPremisesQuery request,
      CancellationToken cancellationToken)
  {
    var theme = await _themeProvider.FindThemeAsync(request.ThemeId, cancellationToken)
        ?? throw new NotFoundException("Univers", request.ThemeId);

    var prompt = _promptBuilder.BuildPremisePrompt(new StoryPremiseRequest(
        theme,
        request.Hero.ToProfile(),
        request.ReaderMode,
        request.Length,
        request.Count));

    IReadOnlyList<GeneratedPremise> premises;
    try
    {
      premises = await _storyGeneration.GeneratePremisesAsync(prompt, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
    {
      throw new StoryGenerationException(_generationFailedMessage, ex);
    }

    var usable = premises
        .Where(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.Hook))
        .Select(p => new StoryPremiseDto(Truncate(p.Title, AdventureSession.MaxTitleLength), Truncate(p.Hook, AdventureSession.MaxPremiseLength)))
        .DistinctBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
        .Take(request.Count)
        .ToArray();

    return usable.Length > 0 ? usable : throw new StoryGenerationException(_generationFailedMessage);
  }

  private static string Truncate(string value, int maxLength)
  {
    var trimmed = value.Trim();
    return trimmed.Length > maxLength ? trimmed[..maxLength].TrimEnd() : trimmed;
  }
}
