using FableFlow.Application.Abstractions;
using FableFlow.Application.Common.Options;
using FableFlow.Infrastructure.Ai;
using FableFlow.Infrastructure.Ai.Flux;
using FableFlow.Infrastructure.BackgroundJobs;
using FableFlow.Infrastructure.Options;
using FableFlow.Infrastructure.Persistence;
using FableFlow.Infrastructure.Themes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FableFlow.Infrastructure;

/// <summary>Point d'enregistrement DI de la couche Infrastructure.</summary>
public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructure(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services.AddOptions<AzureOpenAIOptions>()
        .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName))
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.Endpoint),
            "La configuration 'AzureOpenAI:Endpoint' est requise.");

    services.AddOptions<FeatureOptions>()
        .Bind(configuration.GetSection(FeatureOptions.SectionName));

    services.AddOptions<FluxImageOptions>()
        .Bind(configuration.GetSection(FluxImageOptions.SectionName));

    services.AddHttpClient();

    services.AddSingleton<IThemePolicyProvider, HardcodedThemePolicyProvider>();
    services.AddSingleton<IAdventureRepository, InMemoryAdventureRepository>();
    services.AddSingleton<IPromptBuilder, PromptBuilder>();
    services.AddSingleton<IStoryGenerationService, AzureOpenAIStoryGenerationService>();

    // File de travaux en arrière-plan (génération d'illustration différée, voir SceneImageJobScheduler
    // côté Application) : un seul singleton concret partagé entre le port (écriture) et le hosted
    // service qui le consomme (lecture, réservée à l'assembly courant).
    services.AddSingleton<ChannelBackgroundJobQueue>();
    services.AddSingleton<IBackgroundJobQueue>(sp => sp.GetRequiredService<ChannelBackgroundJobQueue>());
    services.AddHostedService<QueuedBackgroundJobHostedService>();

    var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>()
        ?? new FeatureOptions();

    if (features.ImageGeneration)
    {
      // FLUX.2-pro (Black Forest Labs) est le fournisseur d'image retenu pour ce projet.
      // AzureOpenAIImageGenerationService reste disponible comme alternative pour un déploiement gpt-image-*.
      services.AddSingleton<IImageGenerationService, FluxImageGenerationService>();
    }
    else
    {
      services.AddSingleton<IImageGenerationService, NullImageGenerationService>();
    }

    return services;
  }
}
