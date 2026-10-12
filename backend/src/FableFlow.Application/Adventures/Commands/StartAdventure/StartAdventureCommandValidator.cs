using FableFlow.Application.Common.Heroes;
using FableFlow.Domain.Entities;
using FluentValidation;

namespace FableFlow.Application.Adventures.Commands.StartAdventure;

public sealed class StartAdventureCommandValidator : AbstractValidator<StartAdventureCommand>
{
  public StartAdventureCommandValidator()
  {
    RuleFor(x => x.ThemeId)
        .NotEmpty().WithMessage("L'univers est requis.")
        .MaximumLength(100);

    RuleFor(x => x.Length)
        .IsInEnum().WithMessage("Longueur d'histoire inconnue.");

    RuleFor(x => x.ReaderMode)
        .IsInEnum().WithMessage("Mode de lecture inconnu.");

    RuleFor(x => x.Hero)
        .NotNull().WithMessage("Le héros est requis.")
        .SetValidator(new HeroInputValidator());

    RuleFor(x => x.Premise)
        .NotEmpty().WithMessage("Une idée d'histoire est requise.")
        .MaximumLength(AdventureSession.MaxPremiseLength)
        .WithMessage($"L'idée d'histoire ne doit pas dépasser {AdventureSession.MaxPremiseLength} caractères.");

    RuleFor(x => x.PremiseTitle)
        .MaximumLength(AdventureSession.MaxTitleLength);
  }
}
