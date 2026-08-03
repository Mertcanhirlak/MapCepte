using Transport.Domain.TransitLines;

namespace Transport.Application.TransitLines;

public interface ITransitLineRepository
{
    Task<TransitLineRepositoryPage> ListAsync(
        TransitLineRepositoryQuery query,
        CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(
        string normalizedCode,
        Guid? excludedTransitLineId,
        CancellationToken cancellationToken);

    Task<TransitLine?> FindByIdAsync(
        Guid transitLineId,
        CancellationToken cancellationToken);

    Task AddAsync(TransitLine transitLineEntity, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
