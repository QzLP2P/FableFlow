using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FableFlow.Domain.Policies;
using FluentAssertions;

namespace FableFlow.Domain.Tests.Policies;

public class ChapterScheduleTests
{
  [Fact]
  public void Short_HasSixChaptersWithChoicesAtTwoAndFour()
  {
    var schedule = ChapterSchedule.For(StoryLength.Short);

    schedule.TotalChapters.Should().Be(6);
    schedule.DecisionChapters.Should().Equal(2, 4);
    Kinds(schedule).Should().Equal(
        ChapterKind.Narrative,
        ChapterKind.Decision,
        ChapterKind.Narrative,
        ChapterKind.Decision,
        ChapterKind.Narrative,
        ChapterKind.Ending);
  }

  [Fact]
  public void Longer_HasTenChaptersWithChoicesAtThreeFiveAndEight()
  {
    var schedule = ChapterSchedule.For(StoryLength.Longer);

    schedule.TotalChapters.Should().Be(10);
    schedule.DecisionChapters.Should().Equal(3, 5, 8);
    Kinds(schedule).Count(k => k == ChapterKind.Decision).Should().Be(3);
    schedule.KindOf(10).Should().Be(ChapterKind.Ending);
  }

  [Theory]
  [InlineData(StoryLength.Short)]
  [InlineData(StoryLength.Longer)]
  public void FirstAndLastChapters_NeverOfferChoices(StoryLength length)
  {
    var schedule = ChapterSchedule.For(length);

    schedule.KindOf(1).Should().Be(ChapterKind.Narrative);
    schedule.KindOf(schedule.TotalChapters).Should().Be(ChapterKind.Ending);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(7)]
  public void KindOf_OutsideStory_Throws(int chapterNumber)
  {
    var act = () => ChapterSchedule.For(StoryLength.Short).KindOf(chapterNumber);

    act.Should().Throw<DomainException>();
  }

  private static ChapterKind[] Kinds(ChapterSchedule schedule) =>
      [.. Enumerable.Range(1, schedule.TotalChapters).Select(schedule.KindOf)];
}
