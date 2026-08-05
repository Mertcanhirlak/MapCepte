using Transport.Application.Vehicles;
using Transport.Domain.Vehicles;

namespace Transport.UnitTests;

public sealed class VehicleTrackingServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 5, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IngestsVehiclePositionSuccessfully()
    {
        var repository = new FakeVehiclePositionRepository();
        var service = new VehicleTrackingService(repository, new FixedTimeProvider(FixedNow));

        var command = new IngestVehiclePositionCommand(
            VehicleCode: "BUS-06-101",
            TransitLineId: Guid.NewGuid(),
            RoutePathId: null,
            Longitude: 32.8597,
            Latitude: 39.9208,
            SpeedKmh: 45.5,
            Heading: 180.0);

        var result = await service.IngestPositionAsync(command);

        Assert.NotNull(result);
        Assert.Equal("BUS-06-101", result.VehicleCode);
        Assert.Equal(32.8597, result.Longitude);
        Assert.Equal(39.9208, result.Latitude);
        Assert.Equal(45.5, result.SpeedKmh);
        Assert.Equal(180.0, result.Heading);
        Assert.Equal(FixedNow, result.RecordedAtUtc);
    }

    [Fact]
    public async Task ReturnsLatestVehiclePositionsForLine()
    {
        var repository = new FakeVehiclePositionRepository();
        var service = new VehicleTrackingService(repository, new FixedTimeProvider(FixedNow));
        var lineId = Guid.NewGuid();

        await service.IngestPositionAsync(new IngestVehiclePositionCommand(
            "BUS-01", lineId, null, 32.85, 39.92, 40, 90));
        await service.IngestPositionAsync(new IngestVehiclePositionCommand(
            "BUS-02", lineId, null, 32.86, 39.93, 50, 90));

        var positions = await service.GetLatestPositionsByLineAsync(lineId);

        Assert.Equal(2, positions.Count);
        Assert.Contains(positions, p => p.VehicleCode == "BUS-01");
        Assert.Contains(positions, p => p.VehicleCode == "BUS-02");
    }

    private sealed class FakeVehiclePositionRepository : IVehiclePositionRepository
    {
        public List<VehiclePosition> Items { get; } = [];

        public Task AddAsync(VehiclePosition position, CancellationToken cancellationToken)
        {
            Items.Add(position);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<VehiclePosition>> GetLatestPositionsByLineAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<VehiclePosition> list = Items
                .Where(v => v.TransitLineId == transitLineId)
                .GroupBy(v => v.VehicleCode)
                .Select(g => g.OrderByDescending(v => v.RecordedAtUtc).First())
                .ToList();

            return Task.FromResult(list);
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
