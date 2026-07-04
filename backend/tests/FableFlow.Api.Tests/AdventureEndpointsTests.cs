using System.Net;
using System.Net.Http.Json;
using FableFlow.Application.Adventures.Dtos;
using FluentAssertions;

namespace FableFlow.Api.Tests;

public class AdventureEndpointsTests : IClassFixture<FableFlowWebApplicationFactory>
{
  private readonly HttpClient _client;

  public AdventureEndpointsTests(FableFlowWebApplicationFactory factory) =>
      _client = factory.CreateClient();

  [Fact]
  public async Task StartAdventure_WithValidTheme_ReturnsCreatedWithInitialScene()
  {
    var response = await _client.PostAsJsonAsync("/api/adventures", new StartAdventureRequest("pokemon", 3));

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var adventure = await response.Content.ReadFromJsonAsync<AdventureDto>();

    adventure.Should().NotBeNull();
    adventure!.Status.Should().Be("InProgress");
    adventure.CurrentScene.Should().NotBeNull();
    adventure.CurrentScene!.SceneNumber.Should().Be(1);
    adventure.CurrentScene.Choices.Should().HaveCount(2);
  }

  [Fact]
  public async Task StartAdventure_WithUnknownTheme_ReturnsNotFound()
  {
    var response = await _client.PostAsJsonAsync("/api/adventures", new StartAdventureRequest("does-not-exist", 3));

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task StartAdventure_WithInvalidSceneCount_ReturnsBadRequest()
  {
    var response = await _client.PostAsJsonAsync("/api/adventures", new StartAdventureRequest("pokemon", 1));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GetAdventure_AfterStart_ReturnsCurrentState()
  {
    var started = await StartAdventureAsync(sceneCount: 3);

    var response = await _client.GetAsync($"/api/adventures/{started.AdventureId}");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var adventure = await response.Content.ReadFromJsonAsync<AdventureDto>();
    adventure!.AdventureId.Should().Be(started.AdventureId);
  }

  [Fact]
  public async Task GetAdventure_WithUnknownId_ReturnsNotFound()
  {
    var response = await _client.GetAsync($"/api/adventures/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task MakeChoice_AlwaysGoodChoice_EventuallyWinsAtTargetScene()
  {
    var started = await StartAdventureAsync(sceneCount: 3);
    var adventureId = started.AdventureId;

    AdventureDto current = started;
    while (current.Status == "InProgress")
    {
      var response = await _client.PostAsJsonAsync(
          $"/api/adventures/{adventureId}/choices",
          new MakeChoiceRequest("a"));

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      current = (await response.Content.ReadFromJsonAsync<AdventureDto>())!;
    }

    current.Status.Should().Be("Won");
    current.CurrentScene.Should().BeNull();
    current.OutcomeMessage.Should().NotBeNullOrWhiteSpace();
  }

  [Fact]
  public async Task MakeChoice_ThreeBadChoicesInARow_ResultsInLostBeforeTargetScene()
  {
    var started = await StartAdventureAsync(sceneCount: 10);
    var adventureId = started.AdventureId;

    AdventureDto? current = null;
    for (var i = 0; i < 3; i++)
    {
      var response = await _client.PostAsJsonAsync(
          $"/api/adventures/{adventureId}/choices",
          new MakeChoiceRequest("b"));

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      current = await response.Content.ReadFromJsonAsync<AdventureDto>();
    }

    current!.Status.Should().Be("Lost");
    current.CurrentScene.Should().BeNull();
  }

  [Fact]
  public async Task MakeChoice_WithUnknownChoiceId_ReturnsBadRequest()
  {
    var started = await StartAdventureAsync(sceneCount: 3);

    var response = await _client.PostAsJsonAsync(
        $"/api/adventures/{started.AdventureId}/choices",
        new MakeChoiceRequest("does-not-exist"));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GetHistory_AfterPlayingScenes_ReturnsPlayedScenes()
  {
    var started = await StartAdventureAsync(sceneCount: 3);

    await _client.PostAsJsonAsync(
        $"/api/adventures/{started.AdventureId}/choices",
        new MakeChoiceRequest("a"));

    var response = await _client.GetAsync($"/api/adventures/{started.AdventureId}/history");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var history = await response.Content.ReadFromJsonAsync<AdventureHistoryDto>();
    history!.Scenes.Should().HaveCountGreaterThanOrEqualTo(2);
  }

  private async Task<AdventureDto> StartAdventureAsync(int sceneCount)
  {
    var response = await _client.PostAsJsonAsync("/api/adventures", new StartAdventureRequest("pokemon", sceneCount));
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AdventureDto>())!;
  }
}
