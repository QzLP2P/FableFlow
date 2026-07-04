using FableFlow.Domain.Entities;
using FluentValidation;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

public sealed class StartAdventureCommandValidator : AbstractValidator<StartAdventureCommand>
{
  public StartAdventureCommandValidator()
  {
    RuleFor(x => x.ThemeId)
        .NotEmpty().WithMessage("Le thème est requis.")
        .MaximumLength(100);

    RuleFor(x => x.SceneCount)
        .InclusiveBetween(AdventureSession.MinSceneCount, AdventureSession.MaxSceneCount)
        .WithMessage(
            $"Le nombre de scènes doit être compris entre {AdventureSession.MinSceneCount} et {AdventureSession.MaxSceneCount}.");
  }
}
