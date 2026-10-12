using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FluentAssertions;

namespace FableFlow.Domain.Tests.Entities;

public class HeroProfileTests
{
  [Fact]
  public void Create_NormalizesWhitespaceAndDropsEmptyOptionalValues()
  {
    var hero = HeroProfile.Create("  Léa   Rose ", 7, ["  Max,  son chien ", " ", ""], interests: "  ", appearance: " cheveux bouclés ");

    hero.Name.Should().Be("Léa Rose");
    hero.Age.Should().Be(7);
    hero.Companions.Should().Equal("Max, son chien");
    hero.Interests.Should().BeNull();
    hero.Appearance.Should().Be("cheveux bouclés");
  }

  [Theory]
  [InlineData(HeroProfile.MinAge - 1)]
  [InlineData(HeroProfile.MaxAge + 1)]
  public void Create_WithAgeOutOfRange_Throws(int age)
  {
    var act = () => HeroProfile.Create("Léa", age);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void Create_WithoutName_Throws()
  {
    var act = () => HeroProfile.Create("  ", 5);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void Create_WithTooManyCompanions_Throws()
  {
    var act = () => HeroProfile.Create("Léa", 5, ["a", "b", "c", "d"]);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void Create_WithTooLongDetail_Throws()
  {
    var act = () => HeroProfile.Create("Léa", 5, personality: new string('x', HeroProfile.MaxDetailLength + 1));

    act.Should().Throw<DomainException>();
  }
}

public class AdventureChapterTests
{
  [Fact]
  public void NarrativeChapter_WithChoices_IsRejected()
  {
    var act = () => new AdventureChapter(1, ChapterKind.Narrative, "Texte", [new ChapterChoice("a", "Aller")]);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void EndingChapter_WithChoices_IsRejected()
  {
    var act = () => new AdventureChapter(6, ChapterKind.Ending, "Fin", [new ChapterChoice("a", "Aller")]);

    act.Should().Throw<DomainException>();
  }

  [Theory]
  [InlineData(1)]
  [InlineData(4)]
  public void DecisionChapter_WithWrongChoiceCount_IsRejected(int count)
  {
    var choices = Enumerable.Range(0, count).Select(i => new ChapterChoice($"c{i}", $"Action {i}")).ToArray();

    var act = () => new AdventureChapter(2, ChapterKind.Decision, "Texte", choices);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void DecisionChapter_WithDuplicateChoices_IsRejected()
  {
    var act = () => new AdventureChapter(
        2, ChapterKind.Decision, "Texte", [new ChapterChoice("a", "Aller"), new ChapterChoice("A", "Rester")]);

    act.Should().Throw<DomainException>();
  }
}
