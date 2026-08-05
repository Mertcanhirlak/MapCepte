using NetTopologySuite.Geometries;

namespace Transport.Domain.Vehicles;

public sealed class VehiclePosition
{
    private VehiclePosition()
    {
    }

    public VehiclePosition(
        Guid id,
        string vehicleCode,
        Guid transitLineId,
        Guid? routePathId,
        Point location,
        double? speedKmh,
        double? heading,
        DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Vehicle position id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(vehicleCode))
        {
            throw new ArgumentException("Vehicle code cannot be empty.", nameof(vehicleCode));
        }

        if (transitLineId == Guid.Empty)
        {
            throw new ArgumentException("Transit line id cannot be empty.", nameof(transitLineId));
        }

        ArgumentNullException.ThrowIfNull(location);

        if (location.SRID != 4326)
        {
            throw new ArgumentException("Location point must use SRID 4326.", nameof(location));
        }

        Id = id;
        VehicleCode = vehicleCode.Trim();
        TransitLineId = transitLineId;
        RoutePathId = routePathId;
        Location = location;
        SpeedKmh = speedKmh;
        Heading = heading;
        RecordedAtUtc = recordedAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public string VehicleCode { get; private set; } = string.Empty;

    public Guid TransitLineId { get; private set; }

    public Guid? RoutePathId { get; private set; }

    public Point Location { get; private set; } = default!;

    public double? SpeedKmh { get; private set; }

    public double? Heading { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }
}
