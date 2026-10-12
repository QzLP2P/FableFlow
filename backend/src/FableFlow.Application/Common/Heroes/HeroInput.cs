using FableFlow.Domain.Entities;
using FluentValidation;

namespace FableFlow.Application.Common.Heroes;

/// <summary>Héros saisi par le parent (prénom et âge requis, le reste est facultatif).</summary>
public sealed record HeroInput(
    string Name,
    int Age,
    IReadOnlyList<string>? Companions = null,
    string? Interests = null,
    string? Personality = null,
    string? Appearance = null)
{
  public HeroProfile ToProfile() =>
      HeroProfile.Create(Name, Age, Companions, Interests, Personality, Appearance);
}

/// <summary>Validation des bornes du héros, partagées avec le domaine.</summary>
public sealed class HeroInputValidator : AbstractValidator<HeroInput>
{
  public HeroInputValidator()
  {
    RuleFor(x => x.Name)
        .NotEmpty().WithMessage("Le prénom du héros est requis.")
        .MaximumLength(HeroProfile.MaxNameLength)
        .WithMessage($"Le prénom ne doit pas dépasser {HeroProfile.MaxNameLength} caractères.");

    RuleFor(x => x.Age)
        .InclusiveBetween(HeroProfile.MinAge, HeroProfile.MaxAge)
        .WithMessage($"L'âge doit être compris entre {HeroProfile.MinAge} et {HeroProfile.MaxAge} ans.");

    RuleFor(x => x.Companions)
        .Must(c => c is null || c.Count <= HeroProfile.MaxCompanions)
        .WithMessage($"Au plus {HeroProfile.MaxCompanions} compagnons.");

    RuleForEach(x => x.Companions)
        .MaximumLength(HeroProfile.MaxCompanionLength)
        .WithMessage($"Chaque compagnon doit faire au plus {HeroProfile.MaxCompanionLength} caractères.");

    RuleFor(x => x.Interests).MaximumLength(HeroProfile.MaxDetailLength);
    RuleFor(x => x.Personality).MaximumLength(HeroProfile.MaxDetailLength);
    RuleFor(x => x.Appearance).MaximumLength(HeroProfile.MaxDetailLength);
  }
}
