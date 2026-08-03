using Microsoft.EntityFrameworkCore;
using Transport.Application.TransitLines;
using Transport.Domain.TransitLines;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.TransitLines;

public sealed class EfTransitLineRepository(TransportDbContext dbContext)
    : ITransitLineRepository
{
    public async Task<TransitLineRepositoryPage> ListAsync(
        TransitLineRepositoryQuery query,
        CancellationToken cancellationToken)
    {
        var linesQuery = dbContext.TransitLines
            .Include(line => line.Stops)
            .AsNoTracking();

        linesQuery = query.Scope switch
        {
            TransitLineVisibilityScope.All => linesQuery,
            TransitLineVisibilityScope.Owned => linesQuery.Where(line =>
                line.OwnerUserId == query.ActorUserId),
            TransitLineVisibilityScope.Published => linesQuery.Where(line =>
                line.Status == TransitLineStatus.Published),
            _ => throw new ArgumentOutOfRangeException(nameof(query)),
        };

        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLikePattern(query.Search)}%";
            linesQuery = linesQuery.Where(line =>
                EF.Functions.ILike(line.Name, pattern, "\\")
                || EF.Functions.ILike(line.Code, pattern, "\\"));
        }

        var totalCount = await linesQuery.CountAsync(cancellationToken);
        var items = await linesQuery
            .OrderBy(line => line.Name)
            .ThenBy(line => line.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new TransitLineRepositoryPage(items, totalCount);
    }

    public Task<bool> CodeExistsAsync(
        string normalizedCode,
        Guid? excludedTransitLineId,
        CancellationToken cancellationToken)
    {
        return dbContext.TransitLines.AnyAsync(
            line => line.NormalizedCode == normalizedCode
                && (!excludedTransitLineId.HasValue
                    || line.Id != excludedTransitLineId.Value),
            cancellationToken);
    }

    public Task<TransitLine?> FindByIdAsync(
        Guid transitLineId,
        CancellationToken cancellationToken)
    {
        return dbContext.TransitLines
            .Include(line => line.Stops)
            .SingleOrDefaultAsync(
                line => line.Id == transitLineId,
                cancellationToken);
    }

    public async Task AddAsync(
        TransitLine transitLineEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.TransitLines.AddAsync(transitLineEntity, cancellationToken);
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

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
