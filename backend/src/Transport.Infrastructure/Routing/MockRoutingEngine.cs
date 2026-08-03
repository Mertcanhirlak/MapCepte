using NetTopologySuite.Geometries;
using Transport.Application.Routing;

namespace Transport.Infrastructure.Routing;

public sealed class MockRoutingEngine : IRoutingEngine
{
    public string Name => "MockRoutingEngine";

    public Task<RoutingResult> GenerateRouteAsync(
        IReadOnlyList<RoutingWaypoint> waypoints,
        string profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(waypoints);

        if (waypoints.Count < 2)
        {
            return Task.FromResult(new RoutingResult(
                Success: false,
                FailureCode: "InsufficientWaypoints",
                FailureMessage: "At least 2 waypoints are required to generate a route."));
        }

        var coordinates = new List<Coordinate>();
        double totalDistanceMeters = 0;

        for (var i = 0; i < waypoints.Count - 1; i++)
        {
            var start = waypoints[i];
            var end = waypoints[i + 1];

            var segDistance = CalculateApproximateDistanceMeters(
                start.Longitude, start.Latitude,
                end.Longitude, end.Latitude);

            totalDistanceMeters += segDistance;

            coordinates.Add(new Coordinate(start.Longitude, start.Latitude));

            // Generate intermediate arc control point for realistic line curve
            var midLng = (start.Longitude + end.Longitude) / 2.0 + (end.Latitude - start.Latitude) * 0.05;
            var midLat = (start.Latitude + end.Latitude) / 2.0 - (end.Longitude - start.Longitude) * 0.05;
            coordinates.Add(new Coordinate(midLng, midLat));
        }

        var last = waypoints[^1];
        coordinates.Add(new Coordinate(last.Longitude, last.Latitude));

        var lineString = new LineString(coordinates.ToArray())
        {
            SRID = 4326,
        };

        // Assume average speed of 40 km/h (11.11 m/s)
        var durationSeconds = totalDistanceMeters / 11.11;

        return Task.FromResult(new RoutingResult(
            Success: true,
            Geometry: lineString,
            DistanceMeters: Math.Round(totalDistanceMeters, 2),
            DurationSeconds: Math.Round(durationSeconds, 2)));
    }

    private static double CalculateApproximateDistanceMeters(
        double lon1, double lat1, double lon2, double lat2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
