using FableFlow.Api.Tests.Fakes;
using FableFlow.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FableFlow.Api.Tests;

/// <summary>
/// Factory de test remplaçant les ports IA par des implémentations déterministes,
/// afin de tester l'API sans dépendance réseau vers Azure OpenAI.
/// </summary>
public sealed class FableFlowWebApplicationFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
      services.RemoveAll<IStoryGenerationService>();
      services.AddSingleton<IStoryGenerationService, FakeStoryGenerationService>();
    });
  }
}
