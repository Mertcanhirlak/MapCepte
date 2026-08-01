using Transport.Domain.Stops;

namespace Transport.Application.Stops;

public interface IStopRepository
{
    Task<IReadOnlyCollection<Stop>> ListAsync(
        Guid actorUserId,
        StopVisibilityScope scope,
        CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(
        string normalizedCode,
        Guid? excludedStopId,
        CancellationToken cancellationToken);

    Task<Stop?> FindByIdAsync(
        Guid stopId,
        CancellationToken cancellationToken);

    Task AddAsync(Stop stopEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
