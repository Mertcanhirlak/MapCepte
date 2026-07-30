namespace Transport.Application.Identity;

public sealed class AuditCatalogService(IAuditStore auditStore)
{
    public Task<IReadOnlyCollection<AuditCatalogItem>> ListRecentAsync(
        CancellationToken cancellationToken = default)
    {
        return auditStore.ListRecentAsync(100, cancellationToken);
    }
}
