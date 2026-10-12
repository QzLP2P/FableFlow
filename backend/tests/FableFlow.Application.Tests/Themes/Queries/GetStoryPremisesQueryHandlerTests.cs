using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Tests.TestSupport;
using FableFlow.Application.Themes.Queries.GetStoryPremises;
using FableFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FableFlow.Application.Tests.Themes.Queries;

public class GetStoryPremisesQueryHandlerTests
{
  private readonly IThemePolicyProvider _themeProvider = Substitute.For<IThemePolicyProvider>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();
  private readonly GetStoryPremisesQueryHandler _handler;

  public GetStoryPremisesQueryHandlerTests()
  {
    _themeProvider.FindThemeAsync("dinosaur", Arg.Any<CancellationToken>()).Returns(TestData.Theme());
    _promptBuilder.BuildPremisePrompt(default!).ReturnsForAnyArgs(new StoryPremisePrompt("s", "u", "story-premises@test", 3));
    _handler = new GetStoryPremisesQueryHandler(_themeProvider, _promptBuilder, _storyGeneration);
  }

  [Fact]
  public async Task PersonalizesPromptWithHeroReaderModeAndLength()
  {
    _storyGeneration.GeneratePremisesAsync(default!, default).ReturnsForAnyArgs([new GeneratedPremise("A", "Accroche")]);

    await _handler.Handle(Query(), CancellationToken.None);

    _promptBuilder.Received(1).BuildPremisePrompt(Arg.Is<StoryPremiseRequest>(r =>
        r.Hero.Name == "Léa"
        && r.Hero.Age == 6
        && r.Hero.Interests == "les étoiles"
        && r.ReaderMode == ReaderMode.ChildReader
        && r.Length == StoryLength.Longer
        && r.Count == 3));
  }

  [Fact]
  public async Task DropsEmptyAndDuplicatePremisesAndHonorsCount()
  {
    _storyGeneration.GeneratePremisesAsync(default!, default).ReturnsForAnyArgs(
    [
      new GeneratedPremise(" Le grand œuf ", " Un œuf roule. "),
      new GeneratedPremise("le grand œuf", "Doublon"),
      new GeneratedPremise("", "Sans titre"),
      new GeneratedPremise("La source", "Une source chante."),
      new GeneratedPremise("Le volcan", "Un volcan dort."),
      new GeneratedPremise("En trop", "En trop."),
    ]);

    var result = await _handler.Handle(Query(), CancellationToken.None);

    result.Select(p => p.Title).Should().Equal("Le grand œuf", "La source", "Le volcan");
    result[0].Hook.Should().Be("Un œuf roule.");
  }

  [Fact]
  public async Task ProviderFailure_IsReportedAsGenerationFailure()
  {
    _storyGeneration.GeneratePremisesAsync(default!, default).ThrowsAsyncForAnyArgs(new HttpRequestException("internal"));

    var act = () => _handler.Handle(Query(), CancellationToken.None);

    (await act.Should().ThrowAsync<StoryGenerationException>()).Which.Message.Should().NotContain("internal");
  }

  [Fact]
  public async Task NoUsablePremise_IsReportedAsGenerationFailure()
  {
    _storyGeneration.GeneratePremisesAsync(default!, default).ReturnsForAnyArgs([new GeneratedPremise(" ", " ")]);

    var act = () => _handler.Handle(Query(), CancellationToken.None);

    await act.Should().ThrowAsync<StoryGenerationException>();
  }

  [Fact]
  public async Task UnknownTheme_ThrowsNotFound()
  {
    var act = () => _handler.Handle(Query() with { ThemeId = "unknown" }, CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  private static GetStoryPremisesQuery Query() =>
      new("dinosaur", StoryLength.Longer, ReaderMode.ChildReader, TestData.HeroInput());
}
