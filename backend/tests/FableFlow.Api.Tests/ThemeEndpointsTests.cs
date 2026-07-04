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
}
