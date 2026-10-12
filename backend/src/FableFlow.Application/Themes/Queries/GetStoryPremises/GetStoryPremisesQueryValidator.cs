using FableFlow.Application.Common.Heroes;
using FluentValidation;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

public sealed class GetStoryPremisesQueryValidator : AbstractValidator<GetStoryPremisesQuery>
{
  public GetStoryPremisesQueryValidator()
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

    RuleFor(x => x.Count)
        .InclusiveBetween(1, 5)
        .WithMessage("Le nombre de propositions doit être compris entre 1 et 5.");
  }
}
