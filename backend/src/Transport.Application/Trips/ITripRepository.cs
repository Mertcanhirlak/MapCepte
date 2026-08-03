using Transport.Domain.Trips;

namespace Transport.Application.Trips;

public interface ITripRepository
{
    Task<IReadOnlyCollection<Trip>> ListByTransitLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken);

    Task<Trip?> FindByIdAsync(
        Guid tripId,
        CancellationToken cancellationToken);

    Task AddAsync(Trip tripEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
