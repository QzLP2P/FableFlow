using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Common.Mapping;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

public sealed class MakeChoiceCommandHandler
    : IRequestHandler<MakeChoiceCommand, AdventureDto>
{
  private readonly IAdventureRepository _repository;
  private readonly IThemePolicyProvider _themeProvider;
  private readonly IPromptBuilder _promptBuilder;
  private readonly IStoryGenerationService _storyGeneration;
  private readonly IImageGenerationService _imageGeneration;
  private readonly ISceneImageJobScheduler _imageJobScheduler;

  public MakeChoiceCommandHandler(
      IAdventureRepository repository,
      IThemePolicyProvider themeProvider,
      IPromptBuilder promptBuilder,
      IStoryGenerationService storyGeneration,
      IImageGenerationService imageGeneration,
      ISceneImageJobScheduler imageJobScheduler)
  {
    _repository = repository;
    _themeProvider = themeProvider;
    _promptBuilder = promptBuilder;
    _storyGeneration = storyGeneration;
    _imageGeneration = imageGeneration;
    _imageJobScheduler = imageJobScheduler;
  }

  public async Task<AdventureDto> Handle(
      MakeChoiceCommand request,
      CancellationToken cancellationToken)
  {
    var session = await _repository.GetAsync(request.AdventureId, cancellationToken)
        ?? throw new NotFoundException("Aventure", request.AdventureId);

    var theme = await _themeProvider.FindThemeAsync(session.ThemeId, cancellationToken)
        ?? throw new NotFoundException("Thème", session.ThemeId);

    // Le domaine valide le choix, met à jour le compteur d'échecs et le statut.
    var result = session.RecordChoice(request.ChoiceId);
    var selectedChoice = session.CurrentScene!.FindChoice(request.ChoiceId);

    if (result.RequiresNextScene)
    {
      await GenerateContinuationAsync(session, theme, selectedChoice, cancellationToken);
    }
    else
    {
      await GenerateEndingAsync(session, theme, selectedChoice, cancellationToken);
    }

    await _repository.SaveAsync(session, cancellationToken);

    return session.ToDto(_imageGeneration.IsEnabled);
  }

  private async Task GenerateContinuationAsync(
      AdventureSession session,
      ThemeDefinition theme,
      SceneChoice? selectedChoice,
      CancellationToken cancellationToken)
  {
    var generationRequest = new SceneGenerationRequest(theme, session, SceneKind.Continuation, selectedChoice);
    var prompt = _promptBuilder.BuildScenePrompt(generationRequest);
    var generated = await _storyGeneration.GenerateSceneAsync(prompt, cancellationToken);

    var nextScene = generated.ToDomainScene(session.CurrentSceneNumber + 1);
    session.AttachScene(nextScene);
    session.UpdateSummary(generated.UpdatedSummary);

    // Cf. StartAdventureCommandHandler : l'illustration est générée en arrière-plan, sans retarder
    // la réponse contenant le texte et les choix.
    _imageJobScheduler.ScheduleForScene(session.Id, nextScene.SceneNumber, theme, generated.ImagePrompt);
  }

  private async Task GenerateEndingAsync(
      AdventureSession session,
      ThemeDefinition theme,
      SceneChoice? selectedChoice,
      CancellationToken cancellationToken)
  {
    var generationRequest = new SceneGenerationRequest(theme, session, SceneKind.Ending, selectedChoice);
    var prompt = _promptBuilder.BuildScenePrompt(generationRequest);
    var generated = await _storyGeneration.GenerateSceneAsync(prompt, cancellationToken);

    session.UpdateSummary(generated.UpdatedSummary);
    session.SetOutcome(BuildOutcome(session.Status, generated.Text));

    _imageJobScheduler.ScheduleForOutcome(session.Id, theme, generated.ImagePrompt);
  }

  private static AdventureOutcome BuildOutcome(SessionStatus status, string message) => status switch
  {
    SessionStatus.Won => AdventureOutcome.Won(message),
    SessionStatus.Lost => AdventureOutcome.Lost(message),
    SessionStatus.Completed => AdventureOutcome.Completed(message),
    _ => throw new InvalidOperationException($"Statut terminal inattendu : {status}.")
  };
}
