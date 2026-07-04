using System.Collections.Concurrent;
using FableFlow.Application.Abstractions;
using FableFlow.Domain.Entities;

namespace FableFlow.Infrastructure.Persistence;

/// <summary>
/// Persistance en mémoire des sessions d'aventure (MVP).
/// Évolution prévue : Azure Cosmos DB ou Azure SQL derrière la même interface.
/// </summary>
public sealed class InMemoryAdventureRepository : IAdventureRepository
{
  private readonly ConcurrentDictionary<Guid, AdventureSession> _sessions = new();

  public Task<AdventureSession?> GetAsync(Guid id, CancellationToken cancellationToken)
  {
    _sessions.TryGetValue(id, out var session);
    return Task.FromResult(session);
  }

  public Task SaveAsync(AdventureSession session, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(session);
    _sessions[session.Id] = session;
    return Task.CompletedTask;
  }
}
