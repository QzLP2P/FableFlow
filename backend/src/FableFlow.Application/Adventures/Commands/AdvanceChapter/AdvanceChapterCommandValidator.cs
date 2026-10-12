using FluentValidation;

namespace FableFlow.Application.Adventures.Commands.AdvanceChapter;

public sealed class AdvanceChapterCommandValidator : AbstractValidator<AdvanceChapterCommand>
{
  public AdvanceChapterCommandValidator()
  {
    RuleFor(x => x.AdventureId).NotEmpty();

    RuleFor(x => x.FromChapterNumber)
        .GreaterThanOrEqualTo(1).WithMessage("Le numéro de chapitre doit être supérieur ou égal à 1.");
  }
}
