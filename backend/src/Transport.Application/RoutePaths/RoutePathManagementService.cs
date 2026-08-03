using System.Security.Cryptography;
using System.Text;
using NetTopologySuite.Geometries;
using Transport.Application.Routing;
using Transport.Application.Stops;
using Transport.Application.TransitLines;
using Transport.Domain.RoutePaths;

namespace Transport.Application.RoutePaths;

public sealed class RoutePathManagementService(
    IRoutePathRepository routePathRepository,
    ITransitLineRepository transitLineRepository,
    IStopRepository stopRepository,
    ITransitLineAccessPolicy accessPolicy,
    IRoutingEngine routingEngine,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<RoutePathCatalogItem>> ListByTransitLineAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty)
        {
            return [];
        }

        var transitLine = await transitLineRepository.FindByIdAsync(transitLineId, cancellationToken);
        if (transitLine is null || !accessPolicy.CanRead(access, transitLine))
        {
            return [];
        }

        var routePaths = await routePathRepository.ListByTransitLineAsync(transitLineId, cancellationToken);
        return routePaths.Select(ToCatalogItem).ToArray();
    }

    public async Task<RoutePathResult> GetByIdAsync(
        TransitLineAccessContext access,
        Guid routePathId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (routePathId == Guid.Empty)
        {
            return new RoutePathResult(RoutePathManagementStatus.InvalidInput, Error: "Route path id cannot be empty.");
        }

        var routePath = await routePathRepository.FindByIdAsync(routePathId, cancellationToken);
        if (routePath is null)
        {
            return new RoutePathResult(RoutePathManagementStatus.NotFound, Error: "Route path was not found.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(routePath.TransitLineId, cancellationToken);
        if (transitLine is null || !accessPolicy.CanRead(access, transitLine))
        {
            return new RoutePathResult(RoutePathManagementStatus.Forbidden, Error: "You do not have permission to view this route path.");
        }

        return new RoutePathResult(RoutePathManagementStatus.Success, ToCatalogItem(routePath));
    }

    public async Task<RoutePathResult> GenerateAsync(
        GenerateRoutePathCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Access.UserId == Guid.Empty)
        {
            return Invalid("Actor user id cannot be empty.");
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return Invalid("Transit line id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Invalid("Route path name is required.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(command.TransitLineId, cancellationToken);
        if (transitLine is null)
        {
            return new RoutePathResult(RoutePathManagementStatus.NotFound, Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new RoutePathResult(RoutePathManagementStatus.Forbidden, Error: "You do not have permission to manage this transit line.");
        }

        var lineStops = transitLine.Stops.OrderBy(s => s.Sequence).ToList();
        if (lineStops.Count < 2)
        {
            return new RoutePathResult(
                RoutePathManagementStatus.InsufficientStops,
                Error: "A transit line must have at least 2 stops to generate a route.");
        }

        var stopIds = lineStops.Select(s => s.StopId).ToList();
        var stopEntities = await stopRepository.FindByIdsAsync(stopIds, cancellationToken);

        if (stopIds.Any(id => !stopEntities.ContainsKey(id)))
        {
            return new RoutePathResult(
                RoutePathManagementStatus.InvalidInput,
                Error: "One or more stops in the transit line could not be found.");
        }

        var waypoints = lineStops
            .Select(ls =>
            {
                var stop = stopEntities[ls.StopId];
                return new RoutingWaypoint(ls.StopId, ls.Sequence, stop.Location.X, stop.Location.Y);
            })
            .ToList();

        var inputHash = ComputeInputHash(waypoints);
        var now = timeProvider.GetUtcNow();

        var existingPaths = await routePathRepository.ListByTransitLineAsync(command.TransitLineId, cancellationToken);
        var nextVersion = existingPaths.Count == 0 ? 1 : existingPaths.Max(p => p.Version) + 1;

        var routePath = new RoutePath(
            Guid.NewGuid(),
            command.TransitLineId,
            command.Name,
            command.Direction,
            nextVersion,
            command.ColorOverride,
            routingEngine.Name,
            inputHash,
            command.Access.UserId,
            now);

        var routingResult = await routingEngine.GenerateRouteAsync(waypoints, "bus", cancellationToken);

        if (!routingResult.Success || routingResult.Geometry is null)
        {
            routePath.FailGeneration(
                routingResult.FailureCode ?? "UnknownError",
                routingResult.FailureMessage ?? "Routing engine failed to calculate a path.",
                now);

            await routePathRepository.AddAsync(routePath, cancellationToken);
            await routePathRepository.SaveChangesAsync(cancellationToken);

            return new RoutePathResult(
                RoutePathManagementStatus.GenerationFailed,
                ToCatalogItem(routePath),
                Error: routePath.FailureMessage);
        }

        var stopSnapshots = waypoints.Select(w => (w.StopId, w.Sequence, w.Longitude, w.Latitude));
        routePath.CompleteGeneration(
            routingResult.Geometry,
            routingResult.DistanceMeters,
            routingResult.DurationSeconds,
            stopSnapshots,
            now);

        await routePathRepository.AddAsync(routePath, cancellationToken);
        await routePathRepository.SaveChangesAsync(cancellationToken);

        return new RoutePathResult(RoutePathManagementStatus.Success, ToCatalogItem(routePath));
    }

    private static string ComputeInputHash(IReadOnlyList<RoutingWaypoint> waypoints)
    {
        var builder = new StringBuilder();
        foreach (var wp in waypoints)
        {
            builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"{wp.StopId}:{wp.Sequence}:{wp.Longitude:F6},{wp.Latitude:F6};");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

    private static RoutePathResult Invalid(string error) =>
        new(RoutePathManagementStatus.InvalidInput, Error: error);

    private static RoutePathCatalogItem ToCatalogItem(RoutePath path)
    {
        double[][]? coords = null;
        if (path.Geometry is not null)
        {
            coords = path.Geometry.Coordinates
                .Select(c => new[] { c.X, c.Y })
                .ToArray();
        }

        return new RoutePathCatalogItem(
            path.Id,
            path.TransitLineId,
            path.Name,
            path.Direction.ToString(),
            path.Version,
            path.Status.ToString(),
            path.ColorOverride,
            path.DistanceMeters,
            path.DurationSeconds,
            path.RoutingEngine,
            path.GeneratedAtUtc,
            path.FailureCode,
            path.FailureMessage,
            path.Stops.Count,
            coords);
    }
}
