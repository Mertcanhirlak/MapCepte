using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public interface IAuditStore
{
    Task AddAsync(
        AuditEntry auditEntry,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditCatalogItem>> ListRecentAsync(
        int maximumCount,
        CancellationToken cancellationToken);
}
