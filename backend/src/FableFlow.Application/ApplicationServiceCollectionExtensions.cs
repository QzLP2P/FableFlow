using System.Reflection;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Adventures.Services;
using FableFlow.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FableFlow.Application;

/// <summary>Point d'enregistrement DI de la couche Application.</summary>
public static class ApplicationServiceCollectionExtensions
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    var assembly = Assembly.GetExecutingAssembly();

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
    services.AddValidatorsFromAssembly(assembly);

    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    services.AddSingleton<ISceneImageJobScheduler, SceneImageJobScheduler>();

    return services;
  }
}
