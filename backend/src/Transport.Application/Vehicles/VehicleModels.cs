namespace Transport.Application.Vehicles;

public sealed record IngestVehiclePositionCommand(
    string VehicleCode,
    Guid TransitLineId,
    Guid? RoutePathId,
    double Longitude,
    double Latitude,
    double? SpeedKmh,
    double? Heading);

public sealed record VehiclePositionCatalogItem(
    Guid Id,
    string VehicleCode,
    Guid TransitLineId,
    Guid? RoutePathId,
    double Longitude,
    double Latitude,
    double? SpeedKmh,
    double? Heading,
    DateTimeOffset RecordedAtUtc);
