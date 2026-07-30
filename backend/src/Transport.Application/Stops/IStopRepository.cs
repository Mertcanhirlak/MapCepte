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
        CancellationToken cancellationToken);

    Task AddAsync(Stop stopEntity, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
