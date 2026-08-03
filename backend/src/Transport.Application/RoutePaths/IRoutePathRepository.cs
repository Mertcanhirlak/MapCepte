using Transport.Domain.RoutePaths;

namespace Transport.Application.RoutePaths;

public interface IRoutePathRepository
{
    Task<IReadOnlyCollection<RoutePath>> ListByTransitLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken);

    Task<RoutePath?> FindByIdAsync(
        Guid routePathId,
        CancellationToken cancellationToken);

    Task AddAsync(RoutePath routePathEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
