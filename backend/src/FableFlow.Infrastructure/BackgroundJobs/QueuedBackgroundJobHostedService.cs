using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FableFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Exécute en séquence les travaux mis en file par <see cref="ChannelBackgroundJobQueue"/> (génération
/// d'image des scènes et des issues d'aventure). Chaque travail est exécuté dans un try/catch dédié
/// afin qu'une erreur isolée (ex. échec réseau vers le fournisseur d'image) n'interrompe jamais le
/// traitement des travaux suivants.
/// </summary>
public sealed class QueuedBackgroundJobHostedService : BackgroundService
{
  private readonly ChannelBackgroundJobQueue _queue;
  private readonly ILogger<QueuedBackgroundJobHostedService> _logger;

  public QueuedBackgroundJobHostedService(
      ChannelBackgroundJobQueue queue,
      ILogger<QueuedBackgroundJobHostedService> logger)
  {
    _queue = queue;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await foreach (var workItem in _queue.Reader.ReadAllAsync(stoppingToken))
    {
      try
      {
        await workItem(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        // Arrêt normal de l'application : ne pas journaliser comme une erreur.
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Échec d'un travail en arrière-plan (génération d'illustration).");
      }
    }
  }
}
