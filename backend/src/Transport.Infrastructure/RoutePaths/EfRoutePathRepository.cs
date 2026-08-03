using Microsoft.EntityFrameworkCore;
using Transport.Application.RoutePaths;
using Transport.Domain.RoutePaths;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.RoutePaths;

public sealed class EfRoutePathRepository(TransportDbContext dbContext) : IRoutePathRepository
{
    public async Task<IReadOnlyCollection<RoutePath>> ListByTransitLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken)
    {
        var paths = await dbContext.RoutePaths
            .Include(r => r.Stops)
            .AsNoTracking()
            .Where(r => r.TransitLineId == transitLineId)
            .OrderBy(r => r.Version)
            .ToArrayAsync(cancellationToken);

        return paths;
    }

    public Task<RoutePath?> FindByIdAsync(
        Guid routePathId,
        CancellationToken cancellationToken)
    {
        return dbContext.RoutePaths
            .Include(r => r.Stops)
            .SingleOrDefaultAsync(r => r.Id == routePathId, cancellationToken);
    }

    public async Task AddAsync(
        RoutePath routePathEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.RoutePaths.AddAsync(routePathEntity, cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
