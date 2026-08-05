using Transport.Domain.Vehicles;

namespace Transport.Application.Vehicles;

public interface IVehiclePositionRepository
{
    Task AddAsync(VehiclePosition position, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehiclePosition>> GetLatestPositionsByLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
