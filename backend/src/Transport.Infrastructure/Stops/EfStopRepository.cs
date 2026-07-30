using Microsoft.EntityFrameworkCore;
using Transport.Application.Stops;
using Transport.Domain.Stops;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Stops;

public sealed class EfStopRepository(TransportDbContext dbContext)
    : IStopRepository
{
    public async Task<IReadOnlyCollection<Stop>> ListAsync(
        Guid actorUserId,
        StopVisibilityScope scope,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Stops.AsNoTracking();

        query = scope switch
        {
            StopVisibilityScope.All => query,
            StopVisibilityScope.Owned => query.Where(stop =>
                stop.CreatedByUserId == actorUserId),
            StopVisibilityScope.Published => query.Where(stop =>
                stop.Status == StopStatus.Published),
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };

        return await query
            .OrderBy(stop => stop.Name)
            .ThenBy(stop => stop.Code)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        return dbContext.Stops.AnyAsync(
            stop => stop.NormalizedCode == normalizedCode,
            cancellationToken);
    }

    public async Task AddAsync(
        Stop stopEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.Stops.AddAsync(stopEntity, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
