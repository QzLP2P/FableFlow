using System.Net;
using System.Text.Json.Nodes;
using FableFlow.Api.Tests.Fakes;
using FableFlow.Application.Adventures.Dtos;
using FableFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace FableFlow.Api.Tests;

public class AdventureEndpointsTests : IClassFixture<FableFlowWebApplicationFactory>
{
  private const string _premise = "Léa découvre une graine qui chante la nuit.";

  private readonly HttpClient _client;

  public AdventureEndpointsTests(FableFlowWebApplicationFactory factory) =>
      _client = factory.CreateClient();

  [Fact]
  public async Task StartAdventure_WithValidRequest_ReturnsCreatedWithFirstChapter()
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest());

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var adventure = await response.ReadJsonAsync<AdventureDto>();
    response.Headers.Location!.ToString().Should().EndWith($"/api/adventures/{adventure.AdventureId}");

    adventure.Status.Should().Be(SessionStatus.InProgress);
    adventure.Title.Should().Be("La graine qui chante", "le titre de l'idée choisie prime sur celui proposé par le LLM");
    adventure.Hero.Name.Should().Be("Léa");
    adventure.TotalChapters.Should().Be(6);
    adventure.DecisionChapters.Should().Equal(2, 4);
    adventure.CurrentChapterNumber.Should().Be(1);
    adventure.IsGenerating.Should().BeFalse();
    adventure.Chapters.Should().ContainSingle();
    adventure.Chapters[0].Kind.Should().Be(ChapterKind.Narrative);
    adventure.Chapters[0].Choices.Should().BeEmpty();
  }

  [Fact]
  public async Task StartAdventure_WithOwnIdeaAndNoTitle_UsesTheGeneratedTitle()
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest() with { PremiseTitle = null });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    (await response.ReadJsonAsync<AdventureDto>()).Title.Should().Be("Titre généré");
  }

  [Fact]
  public async Task StartAdventure_SerializesEnumsAsStrings()
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest(length: StoryLength.Longer));

    var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    json["status"]!.GetValue<string>().Should().Be("InProgress");
    json["length"]!.GetValue<string>().Should().Be("Longer");
    json["readerMode"]!.GetValue<string>().Should().Be("ChildReader");
    json["chapters"]![0]!["kind"]!.GetValue<string>().Should().Be("Narrative");
    json["chapters"]![0]!["illustration"]!["status"]!.GetValue<string>().Should().Be("NotRequested");
  }

  [Fact]
  public async Task StartAdventure_WithUnknownTheme_ReturnsNotFound()
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest(themeId: "does-not-exist"));

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task StartAdventure_WithInvalidHero_ReturnsValidationProblem()
  {
    var request = NewRequest() with { Hero = ApiTestJson.Hero(name: "", age: 2) };

    var response = await _client.PostJsonAsync("/api/adventures", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var problem = await response.ReadJsonAsync<ValidationProblemDetails>();
    problem.Errors.Keys.Should().Contain(["Hero.Name", "Hero.Age"]);
  }

  [Fact]
  public async Task StartAdventure_WithoutPremise_ReturnsBadRequest()
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest() with { Premise = " " });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task StartAdventure_WithMalformedBody_ReturnsBadRequest()
  {
    using var content = new StringContent("{\"themeId\":", System.Text.Encoding.UTF8, "application/json");

    var response = await _client.PostAsync("/api/adventures", content);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GetAdventure_AfterStart_ReturnsCurrentState()
  {
    var started = await StartAsync();

    var response = await _client.GetAsync($"/api/adventures/{started.AdventureId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    (await response.ReadJsonAsync<AdventureDto>()).AdventureId.Should().Be(started.AdventureId);
  }

  [Fact]
  public async Task GetAdventure_WithUnknownId_ReturnsNotFound()
  {
    var response = await _client.GetAsync($"/api/adventures/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task ReadingAShortStory_ToTheEnd_CompletesWithAConclusion()
  {
    var adventure = await StartAsync();

    while (adventure.Status == SessionStatus.InProgress)
    {
      var frontier = adventure.Chapters[^1];
      var response = frontier.Kind == ChapterKind.Decision
          ? await ChooseAsync(adventure.AdventureId, frontier.Number, frontier.Choices[1].Id)
          : await AdvanceAsync(adventure.AdventureId, frontier.Number);

      response.StatusCode.Should().Be(HttpStatusCode.OK);
      adventure = await response.ReadJsonAsync<AdventureDto>();
    }

    adventure.Status.Should().Be(SessionStatus.Completed);
    adventure.Chapters.Select(c => c.Kind).Should().Equal(
        ChapterKind.Narrative,
        ChapterKind.Decision,
        ChapterKind.Narrative,
        ChapterKind.Decision,
        ChapterKind.Narrative,
        ChapterKind.Ending);
    adventure.Chapters.Where(c => c.Kind == ChapterKind.Decision)
        .Should().OnlyContain(c => c.SelectedChoiceId == "b");
    adventure.Chapters[^1].Choices.Should().BeEmpty();

    var afterEnd = await AdvanceAsync(adventure.AdventureId, adventure.CurrentChapterNumber);
    afterEnd.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task Advance_Twice_FromTheSameChapter_ReturnsConflict()
  {
    var started = await StartAsync();

    var first = await AdvanceAsync(started.AdventureId, 1);
    var duplicate = await AdvanceAsync(started.AdventureId, 1);

    first.StatusCode.Should().Be(HttpStatusCode.OK);
    duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var problem = await duplicate.ReadJsonAsync<ProblemDetails>();
    problem.Status.Should().Be((int)HttpStatusCode.Conflict);
    problem.Title.Should().NotBeNullOrWhiteSpace();
  }

  [Fact]
  public async Task Advance_FromADecisionChapter_ReturnsConflict()
  {
    var started = await StartAsync();
    await AdvanceAsync(started.AdventureId, 1);

    var response = await AdvanceAsync(started.AdventureId, 2);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task MakeChoice_OnANarrativeChapter_ReturnsConflict()
  {
    var started = await StartAsync();

    var response = await ChooseAsync(started.AdventureId, 1, "a");

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task MakeChoice_WithUnknownChoiceId_ReturnsBadRequest()
  {
    var started = await StartAsync();
    await AdvanceAsync(started.AdventureId, 1);

    var response = await ChooseAsync(started.AdventureId, 2, "z");

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Advance_OnUnknownAdventure_ReturnsNotFound()
  {
    var response = await AdvanceAsync(Guid.NewGuid(), 1);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task Advance_WhenGenerationFails_ReturnsBadGatewayAndKeepsTheStoryRetryable()
  {
    var started = await StartAsync(premise: $"{_premise} {FakeStoryGenerationService.FailFromChapterTwoMarker}");

    var response = await AdvanceAsync(started.AdventureId, 1);

    response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    var body = await response.Content.ReadAsStringAsync();
    body.Should().NotContain("détail interne");

    var current = await (await _client.GetAsync($"/api/adventures/{started.AdventureId}")).ReadJsonAsync<AdventureDto>();
    current.CurrentChapterNumber.Should().Be(1);
    current.IsGenerating.Should().BeFalse();

    var retry = await AdvanceAsync(started.AdventureId, 1);
    retry.StatusCode.Should().Be(HttpStatusCode.BadGateway, "la réservation a été libérée : l'action reste rejouable");
  }

  [Fact]
  public async Task GetHistory_AfterCompletion_ReturnsChosenPathAndConclusion()
  {
    var adventure = await StartAsync();
    while (adventure.Status == SessionStatus.InProgress)
    {
      var frontier = adventure.Chapters[^1];
      var response = frontier.Kind == ChapterKind.Decision
          ? await ChooseAsync(adventure.AdventureId, frontier.Number, "a")
          : await AdvanceAsync(adventure.AdventureId, frontier.Number);
      adventure = await response.ReadJsonAsync<AdventureDto>();
    }

    var historyResponse = await _client.GetAsync($"/api/adventures/{adventure.AdventureId}/history");

    historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var history = await historyResponse.ReadJsonAsync<AdventureHistoryDto>();
    history.Chapters.Should().HaveCount(6);
    history.Chapters.Where(c => c.Kind == ChapterKind.Decision)
        .Should().OnlyContain(c => c.SelectedChoiceLabel == "Suivre la piste lumineuse");
    history.Conclusion.Should().StartWith("Conclusion chaleureuse");
  }

  [Fact]
  public async Task GetHistory_WithUnknownId_ReturnsNotFound()
  {
    var response = await _client.GetAsync($"/api/adventures/{Guid.NewGuid()}/history");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  private static StartAdventureRequest NewRequest(
      string themeId = "pokemon",
      StoryLength length = StoryLength.Short,
      string premise = _premise) =>
      new(themeId, length, ReaderMode.ChildReader, ApiTestJson.Hero(), premise, "La graine qui chante");

  private async Task<AdventureDto> StartAsync(string premise = _premise)
  {
    var response = await _client.PostJsonAsync("/api/adventures", NewRequest(premise: premise));
    response.EnsureSuccessStatusCode();
    return await response.ReadJsonAsync<AdventureDto>();
  }

  private Task<HttpResponseMessage> AdvanceAsync(Guid adventureId, int fromChapterNumber) =>
      _client.PostJsonAsync($"/api/adventures/{adventureId}/advance", new AdvanceChapterRequest(fromChapterNumber));

  private Task<HttpResponseMessage> ChooseAsync(Guid adventureId, int chapterNumber, string choiceId) =>
      _client.PostJsonAsync($"/api/adventures/{adventureId}/choices", new MakeChoiceRequest(chapterNumber, choiceId));
}
