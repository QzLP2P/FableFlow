using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Tests.TestSupport;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FableFlow.Application.Tests.Adventures.Services;

public class ChapterImageJobSchedulerTests
{
  private readonly CapturingQueue _queue = new();
  private readonly IAdventureRepository _repository = Substitute.For<IAdventureRepository>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IImageGenerationService _imageGeneration = Substitute.For<IImageGenerationService>();
  private readonly ChapterImageJobScheduler _scheduler;
  private readonly AdventureSession _session = TestData.Opened();

  public ChapterImageJobSchedulerTests()
  {
    _imageGeneration.IsEnabled.Returns(true);
    _promptBuilder.BuildImagePrompt(default!, default!, default!).ReturnsForAnyArgs(new StoryImagePrompt("p", "s"));
    _repository.GetAsync(_session.Id, Arg.Any<CancellationToken>()).Returns(_session);
    _scheduler = new ChapterImageJobScheduler(
        _queue, _repository, _promptBuilder, _imageGeneration, NullLogger<ChapterImageJobScheduler>.Instance);
  }

  [Fact]
  public void WhenDisabled_DoesNothing()
  {
    _imageGeneration.IsEnabled.Returns(false);

    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "Une vallée");

    _queue.Items.Should().BeEmpty();
    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.NotRequested);
  }

  [Fact]
  public async Task WhenImageIsGenerated_MarksPendingThenReady()
  {
    _imageGeneration.GenerateImageAsync(default!, default).ReturnsForAnyArgs("https://img/1.png");

    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "Une vallée");
    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Pending);

    await _queue.RunAllAsync();

    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Ready);
    _session.Chapters[0].ImageUrl.Should().Be("https://img/1.png");
    await _repository.Received(1).SaveAsync(_session, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UsesStableHeroDescription()
  {
    _imageGeneration.GenerateImageAsync(default!, default).ReturnsForAnyArgs("https://img/1.png");

    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "Une vallée");
    await _queue.RunAllAsync();

    _promptBuilder.Received(1).BuildImagePrompt(Arg.Any<ThemeDefinition>(), _session.Hero, "Une vallée");
  }

  [Fact]
  public async Task WhenProviderReturnsNothing_MarksFailed()
  {
    _imageGeneration.GenerateImageAsync(default!, default).ReturnsForAnyArgs((string?)null);

    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "Une vallée");
    await _queue.RunAllAsync();

    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Failed);
  }

  [Fact]
  public async Task WhenProviderThrows_MarksFailed()
  {
    _imageGeneration.GenerateImageAsync(default!, default).ThrowsAsyncForAnyArgs(new HttpRequestException("boom"));

    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "Une vallée");
    await _queue.RunAllAsync();

    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Failed);
  }

  [Fact]
  public void WithoutDescription_MarksFailedImmediately()
  {
    _scheduler.ScheduleForChapter(_session, 1, TestData.Theme(), "  ");

    _queue.Items.Should().BeEmpty();
    _session.Chapters[0].IllustrationStatus.Should().Be(IllustrationStatus.Failed);
  }

  private sealed class CapturingQueue : IBackgroundJobQueue
  {
    public List<Func<CancellationToken, Task>> Items { get; } = [];

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem) => Items.Add(workItem);

    public async Task RunAllAsync()
    {
      foreach (var item in Items)
      {
        await item(CancellationToken.None);
      }
    }
  }
}
