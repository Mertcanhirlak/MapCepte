using NetTopologySuite.Geometries;
using Transport.Application.RoutePaths;
using Transport.Application.Routing;
using Transport.Application.Stops;
using Transport.Application.TransitLines;
using Transport.Domain.RoutePaths;
using Transport.Domain.Stops;
using Transport.Domain.TransitLines;

namespace Transport.UnitTests;

public sealed class RoutePathManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 3, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GeneratesRoutePathWhenTransitLineHasAtLeastTwoStops()
    {
        var routePathRepository = new FakeRoutePathRepository();
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var routingEngine = new FakeRoutingEngine();

        var operatorId = Guid.NewGuid();
        var line = new TransitLine(
            Guid.NewGuid(),
            "Kızılay - Tunalı",
            "M-101",
            null,
            "#FF0000",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        var stop1 = CreateStop("Kızılay", 32.854, 39.920);
        var stop2 = CreateStop("Tunalı", 32.860, 39.905);

        stopRepository.Stops[stop1.Id] = stop1;
        stopRepository.Stops[stop2.Id] = stop2;

        line.AddStop(Guid.NewGuid(), stop1.Id, operatorId, FixedNow);
        line.AddStop(Guid.NewGuid(), stop2.Id, operatorId, FixedNow);
        lineRepository.Items[line.Id] = line;

        var service = CreateService(
            routePathRepository,
            lineRepository,
            stopRepository,
            routingEngine);

        var access = new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true);

        var result = await service.GenerateAsync(
            new GenerateRoutePathCommand(
                access,
                line.Id,
                "Gidiş Rotası",
                RoutePathDirection.Outbound,
                ColorOverride: null));

        Assert.Equal(RoutePathManagementStatus.Success, result.Status);
        Assert.NotNull(result.RoutePath);
        Assert.Equal("Gidiş Rotası", result.RoutePath.Name);
        Assert.Equal("Ready", result.RoutePath.Status);
        Assert.True(result.RoutePath.DistanceMeters > 0);
        Assert.True(result.RoutePath.DurationSeconds > 0);
        Assert.NotNull(result.RoutePath.Coordinates);
        Assert.True(result.RoutePath.Coordinates.Length >= 2);
    }

    [Fact]
    public async Task RejectsRouteGenerationWhenTransitLineHasLessThanTwoStops()
    {
        var routePathRepository = new FakeRoutePathRepository();
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var routingEngine = new FakeRoutingEngine();

        var operatorId = Guid.NewGuid();
        var line = new TransitLine(
            Guid.NewGuid(),
            "Tek Duraklı Hat",
            "M-102",
            null,
            "#00FF00",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        var stop1 = CreateStop("Kızılay", 32.854, 39.920);
        stopRepository.Stops[stop1.Id] = stop1;
        line.AddStop(Guid.NewGuid(), stop1.Id, operatorId, FixedNow);
        lineRepository.Items[line.Id] = line;

        var service = CreateService(
            routePathRepository,
            lineRepository,
            stopRepository,
            routingEngine);

        var access = new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true);

        var result = await service.GenerateAsync(
            new GenerateRoutePathCommand(
                access,
                line.Id,
                "Gidiş Rotası",
                RoutePathDirection.Outbound,
                ColorOverride: null));

        Assert.Equal(RoutePathManagementStatus.InsufficientStops, result.Status);
        Assert.Null(result.RoutePath);
    }

    [Fact]
    public async Task RejectsRouteGenerationWhenOperatorDoesNotOwnLine()
    {
        var routePathRepository = new FakeRoutePathRepository();
        var lineRepository = new FakeTransitLineRepository();
        var stopRepository = new FakeStopRepository();
        var routingEngine = new FakeRoutingEngine();

        var ownerId = Guid.NewGuid();
        var otherOperatorId = Guid.NewGuid();
        var line = new TransitLine(
            Guid.NewGuid(),
            "Hat",
            "H-1",
            null,
            "#000000",
            ownerUserId: ownerId,
            createdByUserId: ownerId,
            createdAtUtc: FixedNow);

        lineRepository.Items[line.Id] = line;

        var service = CreateService(
            routePathRepository,
            lineRepository,
            stopRepository,
            routingEngine);

        var access = new TransitLineAccessContext(otherOperatorId, IsAdmin: false, IsOperator: true);

        var result = await service.GenerateAsync(
            new GenerateRoutePathCommand(
                access,
                line.Id,
                "Gidiş Rotası",
                RoutePathDirection.Outbound,
                ColorOverride: null));

        Assert.Equal(RoutePathManagementStatus.Forbidden, result.Status);
    }

    private static Stop CreateStop(string name, double lng, double lat) =>
        new(
            Guid.NewGuid(),
            name,
            null,
            null,
            "#000000",
            lng,
            lat,
            Guid.NewGuid(),
            FixedNow);

    private static RoutePathManagementService CreateService(
        IRoutePathRepository routePathRepository,
        ITransitLineRepository lineRepository,
        IStopRepository stopRepository,
        IRoutingEngine routingEngine)
    {
        return new RoutePathManagementService(
            routePathRepository,
            lineRepository,
            stopRepository,
            new TransitLineAccessPolicy(),
            routingEngine,
            new FixedTimeProvider(FixedNow));
    }

    private sealed class FakeRoutingEngine : IRoutingEngine
    {
        public string Name => "FakeRoutingEngine";

        public Task<RoutingResult> GenerateRouteAsync(
            IReadOnlyList<RoutingWaypoint> waypoints,
            string profile,
            CancellationToken cancellationToken = default)
        {
            if (waypoints.Count < 2)
            {
                return Task.FromResult(new RoutingResult(false, FailureCode: "InsufficientWaypoints"));
            }

            var coords = waypoints.Select(w => new Coordinate(w.Longitude, w.Latitude)).ToArray();
            var lineString = new LineString(coords) { SRID = 4326 };

            return Task.FromResult(new RoutingResult(
                Success: true,
                Geometry: lineString,
                DistanceMeters: 1500,
                DurationSeconds: 180));
        }
    }

    private sealed class FakeRoutePathRepository : IRoutePathRepository
    {
        public Dictionary<Guid, RoutePath> Items { get; } = [];

        public Task<IReadOnlyCollection<RoutePath>> ListByTransitLineAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RoutePath> list = Items.Values
                .Where(r => r.TransitLineId == transitLineId)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<RoutePath?> FindByIdAsync(Guid routePathId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(routePathId, out var path);
            return Task.FromResult(path);
        }

        public Task AddAsync(RoutePath routePathEntity, CancellationToken cancellationToken)
        {
            Items[routePathEntity.Id] = routePathEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeTransitLineRepository : ITransitLineRepository
    {
        public Dictionary<Guid, TransitLine> Items { get; } = [];

        public Task<TransitLineRepositoryPage> ListAsync(TransitLineRepositoryQuery query, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedTransitLineId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<TransitLine?> FindByIdAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(transitLineId, out var line);
            return Task.FromResult(line);
        }

        public Task AddAsync(TransitLine transitLineEntity, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeStopRepository : IStopRepository
    {
        public Dictionary<Guid, Stop> Stops { get; } = [];

        public Task<StopRepositoryPage> ListAsync(StopRepositoryQuery query, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedStopId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stop?> FindByIdAsync(Guid stopId, CancellationToken cancellationToken)
        {
            Stops.TryGetValue(stopId, out var stop);
            return Task.FromResult(stop);
        }

        public Task<IReadOnlyDictionary<Guid, Stop>> FindByIdsAsync(IEnumerable<Guid> stopIds, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, Stop> dict = stopIds
                .Where(id => Stops.ContainsKey(id))
                .ToDictionary(id => id, id => Stops[id]);
            return Task.FromResult(dict);
        }

        public Task AddAsync(Stop stopEntity, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
