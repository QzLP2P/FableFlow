using FableFlow.Infrastructure.Themes;
using FluentAssertions;

namespace FableFlow.Infrastructure.Tests.Themes;

public sealed class HardcodedThemePolicyProviderTests
{
  private readonly HardcodedThemePolicyProvider _provider = new();

  [Fact]
  public async Task Every_theme_is_child_safe_described_and_makes_the_child_the_hero()
  {
    var themes = await _provider.GetThemesAsync(CancellationToken.None);

    themes.Should().HaveCount(4);
    themes.Should().OnlyContain(t => t.IsForChildren);
    themes.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.Description));
    themes.Should().OnlyContain(t => t.SafetyConstraints.Count > 0);
    themes.Should().OnlyContain(t => t.NarrativeUniverse.StartsWith("Le héros", StringComparison.Ordinal));
  }

  [Theory]
  [InlineData("pokemon", "Sacha")]
  [InlineData("spidey", "Spidey (Peter Parker)")]
  public async Task Franchise_characters_are_allies_rather_than_the_hero(string themeId, string character)
  {
    var theme = await _provider.FindThemeAsync(themeId, CancellationToken.None);

    theme.Should().NotBeNull();
    theme!.NarrativeUniverse.Should().Contain(character);
    theme.NarrativeUniverse.Should().MatchRegex("jamais à (la place du héros|sa place)");
  }

  [Fact]
  public async Task Theme_lookup_is_case_insensitive()
  {
    var theme = await _provider.FindThemeAsync("DINOSAUR", CancellationToken.None);

    theme.Should().NotBeNull();
    theme!.Id.Should().Be("dinosaur");
  }
}
