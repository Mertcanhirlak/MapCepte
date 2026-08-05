using NetTopologySuite.Geometries;
using Transport.Domain.Vehicles;

namespace Transport.Application.Vehicles;

public sealed class VehicleTrackingService(
    IVehiclePositionRepository positionRepository,
    TimeProvider timeProvider)
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public async Task<VehiclePositionCatalogItem> IngestPositionAsync(
        IngestVehiclePositionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.VehicleCode))
        {
            throw new ArgumentException("Vehicle code is required.", nameof(command));
        }

        if (command.TransitLineId == Guid.Empty)
        {
            throw new ArgumentException("Transit line id is required.", nameof(command));
        }

        var now = timeProvider.GetUtcNow();
        var point = GeometryFactory.CreatePoint(new Coordinate(command.Longitude, command.Latitude));

        var position = new VehiclePosition(
            Guid.NewGuid(),
            command.VehicleCode,
            command.TransitLineId,
            command.RoutePathId,
            point,
            command.SpeedKmh,
            command.Heading,
            now);

        await positionRepository.AddAsync(position, cancellationToken);
        await positionRepository.SaveChangesAsync(cancellationToken);

        return ToCatalogItem(position);
    }

    public async Task<IReadOnlyCollection<VehiclePositionCatalogItem>> GetLatestPositionsByLineAsync(
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        if (transitLineId == Guid.Empty) return [];

        var positions = await positionRepository.GetLatestPositionsByLineAsync(transitLineId, cancellationToken);
        return positions.Select(ToCatalogItem).ToArray();
    }

    private static VehiclePositionCatalogItem ToCatalogItem(VehiclePosition position) =>
        new(
            position.Id,
            position.VehicleCode,
            position.TransitLineId,
            position.RoutePathId,
            position.Location.X,
            position.Location.Y,
            position.SpeedKmh,
            position.Heading,
            position.RecordedAtUtc);
}
