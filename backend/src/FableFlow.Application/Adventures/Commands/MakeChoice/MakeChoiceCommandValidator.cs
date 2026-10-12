using FableFlow.Domain.Entities;
using FluentValidation;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

public sealed class MakeChoiceCommandValidator : AbstractValidator<MakeChoiceCommand>
{
  public MakeChoiceCommandValidator()
  {
    RuleFor(x => x.AdventureId).NotEmpty();

    RuleFor(x => x.ChapterNumber)
        .GreaterThanOrEqualTo(1).WithMessage("Le numéro de chapitre doit être supérieur ou égal à 1.");

    RuleFor(x => x.ChoiceId)
        .NotEmpty().WithMessage("Le choix est requis.")
        .MaximumLength(ChapterChoice.MaxIdLength);
  }
}
