using Microsoft.EntityFrameworkCore;
using Transport.Application.Trips;
using Transport.Domain.Trips;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Trips;

public sealed class EfTripRepository(TransportDbContext dbContext) : ITripRepository
{
    public async Task<IReadOnlyCollection<Trip>> ListByTransitLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Trips
            .Include(t => t.StopTimes)
            .AsNoTracking()
            .Where(t => t.TransitLineId == transitLineId)
            .OrderBy(t => t.DepartureTime)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Trip?> FindByIdAsync(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        return dbContext.Trips
            .Include(t => t.StopTimes)
            .SingleOrDefaultAsync(t => t.Id == tripId, cancellationToken);
    }

    public async Task AddAsync(
        Trip tripEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.Trips.AddAsync(tripEntity, cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
