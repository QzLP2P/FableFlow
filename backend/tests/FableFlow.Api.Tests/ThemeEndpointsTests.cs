using System.Net;
using FableFlow.Application.Themes.Dtos;
using FableFlow.Domain.Enums;
using FluentAssertions;

namespace FableFlow.Api.Tests;

public class ThemeEndpointsTests : IClassFixture<FableFlowWebApplicationFactory>
{
  private readonly HttpClient _client;

  public ThemeEndpointsTests(FableFlowWebApplicationFactory factory) =>
      _client = factory.CreateClient();

  [Fact]
  public async Task GetThemes_ReturnsTheAvailableUniversesWithDescriptions()
  {
    var response = await _client.GetAsync("/api/themes");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var themes = await response.ReadJsonAsync<List<ThemeDto>>();
    themes.Select(t => t.Id).Should().Contain(["pokemon", "spidey", "dinosaur", "vet"]);
    themes.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.DisplayName) && !string.IsNullOrWhiteSpace(t.Description));
  }

  [Fact]
  public async Task PostStoryPremises_WithKnownTheme_ReturnsRequestedCount()
  {
    var response = await _client.PostJsonAsync(
        "/api/themes/pokemon/story-premises",
        new StoryPremisesRequest(StoryLength.Short, ReaderMode.AdultReadAloud, ApiTestJson.Hero(), 3));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var premises = await response.ReadJsonAsync<List<StoryPremiseDto>>();
    premises.Should().HaveCount(3);
    premises.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.Hook));
  }

  [Fact]
  public async Task PostStoryPremises_WithUnknownTheme_ReturnsNotFound()
  {
    var response = await _client.PostJsonAsync(
        "/api/themes/does-not-exist/story-premises",
        new StoryPremisesRequest(StoryLength.Short, ReaderMode.AdultReadAloud, ApiTestJson.Hero()));

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task PostStoryPremises_WithInvalidHero_ReturnsBadRequest()
  {
    var response = await _client.PostJsonAsync(
        "/api/themes/pokemon/story-premises",
        new StoryPremisesRequest(StoryLength.Short, ReaderMode.AdultReadAloud, ApiTestJson.Hero(age: 14)));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task PostStoryPremises_WithTooManyRequested_ReturnsBadRequest()
  {
    var response = await _client.PostJsonAsync(
        "/api/themes/pokemon/story-premises",
        new StoryPremisesRequest(StoryLength.Short, ReaderMode.AdultReadAloud, ApiTestJson.Hero(), 6));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GetStoryPremises_IsNoLongerExposed()
  {
    var response = await _client.GetAsync("/api/themes/pokemon/story-premises");

    response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
  }
}
