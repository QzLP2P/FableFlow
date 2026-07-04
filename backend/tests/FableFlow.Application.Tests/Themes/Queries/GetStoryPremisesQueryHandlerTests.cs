using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Common.Exceptions;
using FableFlow.Application.Themes.Queries.GetStoryPremises;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace FableFlow.Application.Tests.Themes.Queries;

public class GetStoryPremisesQueryHandlerTests
{
  private readonly IThemePolicyProvider _themeProvider = Substitute.For<IThemePolicyProvider>();
  private readonly IPromptBuilder _promptBuilder = Substitute.For<IPromptBuilder>();
  private readonly IStoryGenerationService _storyGeneration = Substitute.For<IStoryGenerationService>();

  private readonly GetStoryPremisesQueryHandler _sut;

  public GetStoryPremisesQueryHandlerTests()
  {
    _sut = new GetStoryPremisesQueryHandler(_themeProvider, _promptBuilder, _storyGeneration);
  }

  [Fact]
  public async Task Handle_WithUnknownTheme_ThrowsNotFoundException()
  {
    _themeProvider.FindThemeAsync("unknown", Arg.Any<CancellationToken>())
        .Returns((ThemeDefinition?)null);

    var act = () => _sut.Handle(new GetStoryPremisesQuery("unknown", 3), CancellationToken.None);

    await act.Should().ThrowAsync<NotFoundException>();
  }

  [Fact]
  public async Task Handle_WithKnownTheme_ReturnsGeneratedPremises()
  {
    var theme = CreateTheme();
    _themeProvider.FindThemeAsync("pokemon", Arg.Any<CancellationToken>()).Returns(theme);

    _promptBuilder.BuildPremisePrompt(theme, 3)
        .Returns(new StoryPremisePrompt("system", "user", "v1", 3));

    _storyGeneration.GeneratePremisesAsync(Arg.Any<StoryPremisePrompt>(), Arg.Any<CancellationToken>())
        .Returns(new List<GeneratedPremise>
        {
                    new("Le tournoi des badges", "Un jeune dresseur tente sa chance dans un grand tournoi régional."),
                    new("Le mystère de la forêt", "Une créature rare aurait été aperçue près du village."),
                    new("L'ami disparu", "Un ami d'enfance a disparu sans laisser de trace.")
        });

    var result = await _sut.Handle(new GetStoryPremisesQuery("pokemon", 3), CancellationToken.None);

    result.Should().HaveCount(3);
    result[0].Title.Should().Be("Le tournoi des badges");
  }

  private static ThemeDefinition CreateTheme() => new(
      "pokemon",
      "Pokémon",
      AudienceTarget.Child,
      VocabularyLevel.Simple,
      "Univers de dresseurs",
      ["Aucune violence"],
      "style dessin animé");
}
