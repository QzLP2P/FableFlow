using FableFlow.Application.Abstractions;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Themes.Dtos;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

public sealed class GetStoryPremisesQueryHandler
    : IRequestHandler<GetStoryPremisesQuery, IReadOnlyList<StoryPremiseDto>>
{
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
        ?? throw new NotFoundException("Thème", request.ThemeId);

    var prompt = _promptBuilder.BuildPremisePrompt(theme, request.Count);
    var premises = await _storyGeneration.GeneratePremisesAsync(prompt, cancellationToken);

    return [.. premises.Select(p => new StoryPremiseDto(p.Title, p.Hook))];
  }
}
