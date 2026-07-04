using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using FableFlow.Domain.Entities;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

public sealed class StartAdventureCommandHandler
    : IRequestHandler<StartAdventureCommand, AdventureDto>
{
  private readonly IThemePolicyProvider _themeProvider;
  private readonly IAdventureRepository _repository;
  private readonly IPromptBuilder _promptBuilder;
  private readonly IStoryGenerationService _storyGeneration;
  private readonly IImageGenerationService _imageGeneration;

  public StartAdventureCommandHandler(
      IThemePolicyProvider themeProvider,
      IAdventureRepository repository,
      IPromptBuilder promptBuilder,
      IStoryGenerationService storyGeneration,
      IImageGenerationService imageGeneration)
  {
    _themeProvider = themeProvider;
    _repository = repository;
    _promptBuilder = promptBuilder;
    _storyGeneration = storyGeneration;
    _imageGeneration = imageGeneration;
  }

  public async Task<AdventureDto> Handle(
      StartAdventureCommand request,
      CancellationToken cancellationToken)
  {
    var theme = await _themeProvider.FindThemeAsync(request.ThemeId, cancellationToken)
        ?? throw new NotFoundException("Thème", request.ThemeId);

    var session = AdventureSession.Start(
        Guid.NewGuid(),
        theme.Id,
        request.SceneCount,
        narrativePremise: request.NarrativePremise);

    var generationRequest = new SceneGenerationRequest(theme, session, SceneKind.Initial, SelectedChoice: null);
    var prompt = _promptBuilder.BuildScenePrompt(generationRequest);
    var generated = await _storyGeneration.GenerateSceneAsync(prompt, cancellationToken);

    var scene = generated.ToDomainScene(sceneNumber: 1);
    session.AttachScene(scene);
    session.UpdateSummary(generated.UpdatedSummary);

    await TryAttachImageAsync(session, generationRequest, generated.ImagePrompt, cancellationToken);

    await _repository.SaveAsync(session, cancellationToken);

    return session.ToDto();
  }

  private async Task TryAttachImageAsync(
      AdventureSession session,
      SceneGenerationRequest generationRequest,
      string genericSceneDescription,
      CancellationToken cancellationToken)
  {
    if (!_imageGeneration.IsEnabled)
    {
      return;
    }

    var imagePrompt = _promptBuilder.BuildImagePrompt(generationRequest, genericSceneDescription);
    var imageUrl = await _imageGeneration.GenerateImageAsync(imagePrompt, cancellationToken);
    if (imageUrl is not null)
    {
      session.AttachImageToCurrentScene(imageUrl);
    }
  }
}
