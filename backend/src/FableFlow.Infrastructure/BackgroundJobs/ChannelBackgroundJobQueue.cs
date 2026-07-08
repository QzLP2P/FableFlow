using System.Threading.Channels;
using FableFlow.Application.Abstractions;

namespace FableFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// File de travaux en arrière-plan adossée à un <see cref="Channel{T}"/> non borné. Le port
/// <see cref="IBackgroundJobQueue"/> n'expose que l'écriture (mise en file) ; la lecture (consommation)
/// est réservée à <see cref="QueuedBackgroundJobHostedService"/>, dans le même assembly.
/// </summary>
public sealed class ChannelBackgroundJobQueue : IBackgroundJobQueue
{
  private readonly Channel<Func<CancellationToken, Task>> _channel =
      Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
      {
        SingleReader = true,
        SingleWriter = false,
      });

  public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
  {
    ArgumentNullException.ThrowIfNull(workItem);

    // Canal non borné : TryWrite ne peut échouer que si le canal est complété (jamais le cas ici,
    // aucun appel à _channel.Writer.Complete() n'existe dans ce service).
    _channel.Writer.TryWrite(workItem);
  }

  internal ChannelReader<Func<CancellationToken, Task>> Reader => _channel.Reader;
}
