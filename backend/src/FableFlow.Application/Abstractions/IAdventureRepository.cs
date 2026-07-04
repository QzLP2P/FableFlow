using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions;

/// <summary>Persistance des sessions d'aventure.</summary>
public interface IAdventureRepository
{
    Task<AdventureSession?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(AdventureSession session, CancellationToken cancellationToken);
}
