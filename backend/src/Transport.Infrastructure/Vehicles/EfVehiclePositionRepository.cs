using Microsoft.EntityFrameworkCore;
using Transport.Application.Vehicles;
using Transport.Domain.Vehicles;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Vehicles;

public sealed class EfVehiclePositionRepository(TransportDbContext dbContext)
    : IVehiclePositionRepository
{
    public async Task AddAsync(
        VehiclePosition position,
        CancellationToken cancellationToken)
    {
        await dbContext.VehiclePositions.AddAsync(position, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehiclePosition>> GetLatestPositionsByLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken)
    {
        // Get the latest position recorded for each vehicle on the given line
        return await dbContext.VehiclePositions
            .AsNoTracking()
            .Where(v => v.TransitLineId == transitLineId)
            .GroupBy(v => v.VehicleCode)
            .Select(g => g.OrderByDescending(v => v.RecordedAtUtc).First())
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
