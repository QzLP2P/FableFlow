using FableFlow.Application.Adventures.Commands.StartAdventure;
using FableFlow.Application.Common.Heroes;
using FableFlow.Application.Tests.TestSupport;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;

namespace FableFlow.Application.Tests.Adventures.Commands;

public class StartAdventureCommandValidatorTests
{
  private readonly StartAdventureCommandValidator _validator = new();

  [Fact]
  public void ValidCommand_Passes()
  {
    _validator.Validate(Valid()).IsValid.Should().BeTrue();
  }

  [Theory]
  [InlineData(HeroProfile.MinAge - 1)]
  [InlineData(HeroProfile.MaxAge + 1)]
  public void AgeOutOfRange_Fails(int age)
  {
    var result = _validator.Validate(Valid() with { Hero = TestData.HeroInput() with { Age = age } });

    result.Errors.Should().Contain(e => e.PropertyName == "Hero.Age");
  }

  [Fact]
  public void MissingHero_Fails()
  {
    _validator.Validate(Valid() with { Hero = null! }).Errors.Should().Contain(e => e.PropertyName == "Hero");
  }

  [Fact]
  public void TooManyCompanions_Fails()
  {
    var hero = TestData.HeroInput() with { Companions = ["a", "b", "c", "d"] };

    _validator.Validate(Valid() with { Hero = hero }).IsValid.Should().BeFalse();
  }

  [Fact]
  public void UnknownEnums_Fail()
  {
    var result = _validator.Validate(Valid() with { Length = (StoryLength)42, ReaderMode = (ReaderMode)42 });

    result.Errors.Select(e => e.PropertyName).Should().Contain(["Length", "ReaderMode"]);
  }

  [Fact]
  public void MissingOrTooLongPremise_Fails()
  {
    _validator.Validate(Valid() with { Premise = " " }).IsValid.Should().BeFalse();
    _validator.Validate(Valid() with { Premise = new string('a', AdventureSession.MaxPremiseLength + 1) }).IsValid.Should().BeFalse();
  }

  private static StartAdventureCommand Valid() =>
      new("dinosaur", StoryLength.Short, ReaderMode.AdultReadAloud, new HeroInput("Léa", 6), "Un œuf mystérieux");
}
