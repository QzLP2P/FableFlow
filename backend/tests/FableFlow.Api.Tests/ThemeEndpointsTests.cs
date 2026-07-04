using System.Net;
using System.Net.Http.Json;
using FableFlow.Application.Themes.Dtos;
using FluentAssertions;

namespace FableFlow.Api.Tests;

public class ThemeEndpointsTests : IClassFixture<FableFlowWebApplicationFactory>
{
  private readonly HttpClient _client;

  public ThemeEndpointsTests(FableFlowWebApplicationFactory factory) =>
      _client = factory.CreateClient();

  [Fact]
  public async Task GetThemes_ReturnsOkWithHardcodedThemes()
  {
    var response = await _client.GetAsync("/api/themes");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var themes = await response.Content.ReadFromJsonAsync<List<ThemeDto>>();

    themes.Should().NotBeNull();
    themes!.Select(t => t.Id).Should().Contain(["pokemon", "spidey"]);
  }

  [Fact]
  public async Task GetStoryPremises_WithKnownTheme_ReturnsRequestedCount()
  {
    var response = await _client.GetAsync("/api/themes/pokemon/story-premises?count=3");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var premises = await response.Content.ReadFromJsonAsync<List<StoryPremiseDto>>();

    premises.Should().NotBeNull();
    premises!.Should().HaveCount(3);
    premises.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.Hook));
  }

  [Fact]
  public async Task GetStoryPremises_WithUnknownTheme_ReturnsNotFound()
  {
    var response = await _client.GetAsync("/api/themes/does-not-exist/story-premises");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }
}
