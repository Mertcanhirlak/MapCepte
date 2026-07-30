using Microsoft.EntityFrameworkCore;
using Transport.Application.Identity;
using Transport.Domain.Identity;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Identity;

public sealed class EfAuditStore(TransportDbContext dbContext) : IAuditStore
{
    public async Task AddAsync(
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        await dbContext.AuditEntries.AddAsync(
            auditEntry,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditCatalogItem>> ListRecentAsync(
        int maximumCount,
        CancellationToken cancellationToken)
    {
        return await dbContext.AuditEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(maximumCount)
            .Select(entry => new AuditCatalogItem(
                entry.Id,
                entry.EventType,
                entry.Outcome,
                entry.OccurredAtUtc,
                entry.ActorUserId,
                entry.SubjectUserId,
                entry.IpAddress))
            .ToArrayAsync(cancellationToken);
    }
}
