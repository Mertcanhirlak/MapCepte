using Transport.Domain.Stops;

namespace Transport.Application.Stops;

public interface IStopRepository
{
    Task<StopRepositoryPage> ListAsync(
        StopRepositoryQuery query,
        CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(
        string normalizedCode,
        Guid? excludedStopId,
        CancellationToken cancellationToken);

    Task<Stop?> FindByIdAsync(
        Guid stopId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Stop>> FindByIdsAsync(
        IEnumerable<Guid> stopIds,
        CancellationToken cancellationToken);

    Task AddAsync(Stop stopEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
