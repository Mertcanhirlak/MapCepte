using NetTopologySuite.Geometries;

namespace Transport.Domain.RoutePaths;

public sealed class RoutePathStop
{
    private RoutePathStop()
    {
    }

    internal RoutePathStop(
        Guid id,
        Guid routePathId,
        Guid stopId,
        int sequence,
        double longitude,
        double latitude)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Route path stop id cannot be empty.", nameof(id));
        }

        if (routePathId == Guid.Empty)
        {
            throw new ArgumentException("Route path id cannot be empty.", nameof(routePathId));
        }

        if (stopId == Guid.Empty)
        {
            throw new ArgumentException("Stop id cannot be empty.", nameof(stopId));
        }

        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "Sequence must be greater than zero.");
        }

        Id = id;
        RoutePathId = routePathId;
        StopId = stopId;
        Sequence = sequence;
        Location = CreateLocation(longitude, latitude);
    }

    public Guid Id { get; private set; }

    public Guid RoutePathId { get; private set; }

    public Guid StopId { get; private set; }

    public int Sequence { get; private set; }

    public Point Location { get; private set; } = null!;

    private static Point CreateLocation(double longitude, double latitude)
    {
        if (longitude is < -180.0 or > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be between -180 and 180 degrees.");
        }

        if (latitude is < -90.0 or > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                "Latitude must be between -90 and 90 degrees.");
        }

        return new Point(longitude, latitude)
        {
            SRID = 4326,
        };
    }
}
