using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Transport.Application.Stops;
using Transport.Domain.Stops;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Stops;

public sealed class EfStopRepository(TransportDbContext dbContext)
    : IStopRepository
{
    public async Task<StopRepositoryPage> ListAsync(
        StopRepositoryQuery query,
        CancellationToken cancellationToken)
    {
        var stopsQuery = dbContext.Stops.AsNoTracking();

        stopsQuery = query.Scope switch
        {
            StopVisibilityScope.All => stopsQuery,
            StopVisibilityScope.Owned => stopsQuery.Where(stop =>
                stop.CreatedByUserId == query.ActorUserId),
            StopVisibilityScope.Published => stopsQuery.Where(stop =>
                stop.Status == StopStatus.Published),
            _ => throw new ArgumentOutOfRangeException(nameof(query)),
        };

        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLikePattern(query.Search)}%";
            stopsQuery = stopsQuery.Where(stop =>
                EF.Functions.ILike(stop.Name, pattern, "\\")
                || stop.Code != null
                && EF.Functions.ILike(stop.Code, pattern, "\\"));
        }

        if (query.Bounds is not null)
        {
            var boundingPolygon = CreateBoundingPolygon(query.Bounds);
            stopsQuery = stopsQuery.Where(stop =>
                stop.Location.Intersects(boundingPolygon));
        }

        var totalCount = await stopsQuery.CountAsync(cancellationToken);
        var items = await stopsQuery
            .OrderBy(stop => stop.Name)
            .ThenBy(stop => stop.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new StopRepositoryPage(items, totalCount);
    }

    public Task<bool> CodeExistsAsync(
        string normalizedCode,
        Guid? excludedStopId,
        CancellationToken cancellationToken)
    {
        return dbContext.Stops.AnyAsync(
            stop => stop.NormalizedCode == normalizedCode
                && (!excludedStopId.HasValue
                    || stop.Id != excludedStopId.Value),
            cancellationToken);
    }

    public Task<Stop?> FindByIdAsync(
        Guid stopId,
        CancellationToken cancellationToken)
    {
        return dbContext.Stops.SingleOrDefaultAsync(
            stop => stop.Id == stopId,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, Stop>> FindByIdsAsync(
        IEnumerable<Guid> stopIds,
        CancellationToken cancellationToken)
    {
        var ids = stopIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, Stop>();
        }

        var stops = await dbContext.Stops
            .Where(stop => ids.Contains(stop.Id))
            .ToDictionaryAsync(stop => stop.Id, cancellationToken);

        return stops;
    }

    public async Task AddAsync(
        Stop stopEntity,
        CancellationToken cancellationToken)
    {
        await dbContext.Stops.AddAsync(stopEntity, cancellationToken);
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

    private static Polygon CreateBoundingPolygon(StopBounds bounds)
    {
        return new Polygon(
            new LinearRing(
            [
                new Coordinate(
                    bounds.MinLongitude,
                    bounds.MinLatitude),
                new Coordinate(
                    bounds.MaxLongitude,
                    bounds.MinLatitude),
                new Coordinate(
                    bounds.MaxLongitude,
                    bounds.MaxLatitude),
                new Coordinate(
                    bounds.MinLongitude,
                    bounds.MaxLatitude),
                new Coordinate(
                    bounds.MinLongitude,
                    bounds.MinLatitude),
            ]))
        {
            SRID = 4326,
        };
    }
}
