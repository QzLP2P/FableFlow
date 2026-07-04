using FluentValidation;

namespace FableFlow.Application.Themes.Queries.GetStoryPremises;

public sealed class GetStoryPremisesQueryValidator : AbstractValidator<GetStoryPremisesQuery>
{
  public GetStoryPremisesQueryValidator()
  {
    RuleFor(x => x.ThemeId)
        .NotEmpty().WithMessage("Le thème est requis.")
        .MaximumLength(100);

    RuleFor(x => x.Count)
        .InclusiveBetween(1, 5)
        .WithMessage("Le nombre de propositions doit être compris entre 1 et 5.");
  }
}
