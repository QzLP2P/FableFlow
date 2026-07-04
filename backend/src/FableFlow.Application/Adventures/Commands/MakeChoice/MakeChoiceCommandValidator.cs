using FluentValidation;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

public sealed class MakeChoiceCommandValidator : AbstractValidator<MakeChoiceCommand>
{
    public MakeChoiceCommandValidator()
    {
        RuleFor(x => x.AdventureId)
            .NotEmpty().WithMessage("L'identifiant de l'aventure est requis.");

        RuleFor(x => x.ChoiceId)
            .NotEmpty().WithMessage("Le choix est requis.")
            .MaximumLength(50);
    }
}
