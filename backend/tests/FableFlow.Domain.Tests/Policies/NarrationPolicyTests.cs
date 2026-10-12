using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FableFlow.Domain.Policies;
using FluentAssertions;

namespace FableFlow.Domain.Tests.Policies;

public class NarrationPolicyTests
{
  [Fact]
  public void Age5AndAge10_ProduceContrastingGuidance()
  {
    var five = NarrationPolicy.For(5, ReaderMode.AdultReadAloud);
    var ten = NarrationPolicy.For(10, ReaderMode.AdultReadAloud);

    five.AgeBand.Should().NotBe(ten.AgeBand);
    five.MaxWordsPerChapter.Should().BeLessThan(ten.MinWordsPerChapter);
    five.MaxWordsPerSentence.Should().BeLessThan(ten.MaxWordsPerSentence);
    five.Vocabulary.Should().NotBe(ten.Vocabulary);
    five.Sentences.Should().NotBe(ten.Sentences);
    five.SubjectTreatment.Should().NotBe(ten.SubjectTreatment);
  }

  [Theory]
  [InlineData(5)]
  [InlineData(10)]
  public void ChildReader_IsLighterThanReadAloudForSameAge(int age)
  {
    var readAloud = NarrationPolicy.For(age, ReaderMode.AdultReadAloud);
    var childReader = NarrationPolicy.For(age, ReaderMode.ChildReader);

    childReader.MaxWordsPerChapter.Should().BeLessThan(readAloud.MaxWordsPerChapter);
    childReader.MaxWordsPerSentence.Should().BeLessThan(readAloud.MaxWordsPerSentence);
    childReader.ReaderSupport.Should().NotBe(readAloud.ReaderSupport);
    childReader.Vocabulary.Should().Be(readAloud.Vocabulary, "le mode de lecture complète l'âge sans le remplacer");
  }

  [Fact]
  public void YoungChildReader_GetsBeginnerDecodingSupportWithoutClaimingALevel()
  {
    var guidance = NarrationPolicy.For(6, ReaderMode.ChildReader);

    guidance.ReaderSupport.Should().Contain("orthographe régulière");
    guidance.ReaderSupport.Should().NotContain("CP");
  }

  [Fact]
  public void ReadAloud_MentionsTheHeroAge()
  {
    NarrationPolicy.For(8, ReaderMode.AdultReadAloud).ReaderSupport.Should().Contain("8 ans");
  }

  [Theory]
  [InlineData(2)]
  [InlineData(13)]
  public void OutOfRangeAge_Throws(int age)
  {
    var act = () => NarrationPolicy.For(age, ReaderMode.AdultReadAloud);

    act.Should().Throw<DomainException>();
  }
}
