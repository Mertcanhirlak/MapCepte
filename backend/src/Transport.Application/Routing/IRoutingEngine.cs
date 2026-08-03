using NetTopologySuite.Geometries;

namespace Transport.Application.Routing;

public sealed record RoutingWaypoint(
    Guid StopId,
    int Sequence,
    double Longitude,
    double Latitude);

public sealed record RoutingResult(
    bool Success,
    LineString? Geometry = null,
    double DistanceMeters = 0,
    double DurationSeconds = 0,
    string? FailureCode = null,
    string? FailureMessage = null);

public interface IRoutingEngine
{
    string Name { get; }

    Task<RoutingResult> GenerateRouteAsync(
        IReadOnlyList<RoutingWaypoint> waypoints,
        string profile,
        CancellationToken cancellationToken = default);
}
