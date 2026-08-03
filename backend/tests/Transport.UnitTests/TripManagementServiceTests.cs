using NetTopologySuite.Geometries;
using Transport.Application.Calendars;
using Transport.Application.RoutePaths;
using Transport.Application.TransitLines;
using Transport.Application.Trips;
using Transport.Domain.Calendars;
using Transport.Domain.RoutePaths;
using Transport.Domain.TransitLines;

namespace Transport.UnitTests;

public sealed class TripManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 3, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesTripAndCalculatesProportionalStopETAs()
    {
        var tripRepository = new FakeTripRepository();
        var lineRepository = new FakeTransitLineRepository();
        var routePathRepository = new FakeRoutePathRepository();
        var calendarRepository = new FakeOperatingCalendarRepository();

        var operatorId = Guid.NewGuid();
        var line = new TransitLine(
            Guid.NewGuid(),
            "Kızılay - Tunalı Line",
            "M-201",
            null,
            "#00FF00",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        lineRepository.Items[line.Id] = line;

        var routePath = new RoutePath(
            Guid.NewGuid(),
            line.Id,
            "Gidiş Rotası",
            RoutePathDirection.Outbound,
            version: 1,
            colorOverride: null,
            routingEngine: "MockEngine",
            inputHash: "hash123",
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        var stop1Id = Guid.NewGuid();
        var stop2Id = Guid.NewGuid();
        var stop3Id = Guid.NewGuid();

        routePath.CompleteGeneration(
            new LineString([new Coordinate(32.8, 39.9), new Coordinate(32.85, 39.95)]) { SRID = 4326 },
            distanceMeters: 3000,
            durationSeconds: 600, // 10 mins
            [
                (stop1Id, 1, 32.8, 39.9),
                (stop2Id, 2, 32.82, 39.92),
                (stop3Id, 3, 32.85, 39.95),
            ],
            FixedNow);

        routePathRepository.Items[routePath.Id] = routePath;

        var calendar = new OperatingCalendar(
            Guid.NewGuid(),
            "Hafta İçi",
            DaysOfWeek.Weekdays,
            isActive: true,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        calendarRepository.Items[calendar.Id] = calendar;

        var service = new TripManagementService(
            tripRepository,
            lineRepository,
            routePathRepository,
            calendarRepository,
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));

        var access = new TransitLineAccessContext(operatorId, IsAdmin: false, IsOperator: true);

        var result = await service.CreateTripAsync(
            new CreateTripCommand(
                access,
                line.Id,
                routePath.Id,
                calendar.Id,
                "TRIP-0800",
                new TimeOnly(8, 0),
                RoutePathDirection.Outbound));

        Assert.Equal(TripManagementStatus.Success, result.Status);
        Assert.NotNull(result.Trip);
        Assert.Equal("TRIP-0800", result.Trip.TripCode);
        Assert.Equal(new TimeOnly(8, 0), result.Trip.DepartureTime);
        Assert.Equal(3, result.Trip.StopTimes.Count);

        var stopTimes = result.Trip.StopTimes.OrderBy(s => s.Sequence).ToList();
        Assert.Equal(new TimeOnly(8, 0), stopTimes[0].ArrivalTime); // 0 mins offset
        Assert.Equal(new TimeOnly(8, 5), stopTimes[1].ArrivalTime); // 5 mins offset (50% of 10 min)
        Assert.Equal(new TimeOnly(8, 10), stopTimes[2].ArrivalTime); // 10 mins offset
    }

    [Fact]
    public async Task ShiftsTripTimeByOffset()
    {
        var tripRepository = new FakeTripRepository();
        var lineRepository = new FakeTransitLineRepository();
        var routePathRepository = new FakeRoutePathRepository();
        var calendarRepository = new FakeOperatingCalendarRepository();

        var operatorId = Guid.NewGuid();
        var line = new TransitLine(
            Guid.NewGuid(),
            "Line",
            "L-1",
            null,
            "#000000",
            ownerUserId: operatorId,
            createdByUserId: operatorId,
            createdAtUtc: FixedNow);

        lineRepository.Items[line.Id] = line;

        var routePath = new RoutePath(
            Guid.NewGuid(),
            line.Id,
            "Route",
            RoutePathDirection.Outbound,
            1, null, "Mock", "hash", operatorId, FixedNow);

        var stop1Id = Guid.NewGuid();
        var stop2Id = Guid.NewGuid();
        routePath.CompleteGeneration(
            new LineString([new Coordinate(32.8, 39.9), new Coordinate(32.85, 39.95)]) { SRID = 4326 },
            1000,
            300,
            [(stop1Id, 1, 32.8, 39.9), (stop2Id, 2, 32.85, 39.95)],
            FixedNow);

        routePathRepository.Items[routePath.Id] = routePath;

        var calendar = new OperatingCalendar(Guid.NewGuid(), "Cal", DaysOfWeek.Everyday, true, operatorId, FixedNow);
        calendarRepository.Items[calendar.Id] = calendar;

        var service = new TripManagementService(
            tripRepository,
            lineRepository,
            routePathRepository,
            calendarRepository,
            new TransitLineAccessPolicy(),
            new FixedTimeProvider(FixedNow));

        var access = new TransitLineAccessContext(operatorId, false, true);
        var createResult = await service.CreateTripAsync(new CreateTripCommand(
            access, line.Id, routePath.Id, calendar.Id, "T1", new TimeOnly(10, 0), RoutePathDirection.Outbound));

        var tripId = createResult.Trip!.Id;

        // Shift by +15 minutes
        var shiftResult = await service.ShiftTripTimeAsync(new ShiftTripTimeCommand(access, tripId, 15));

        Assert.Equal(TripManagementStatus.Success, shiftResult.Status);
        Assert.Equal(new TimeOnly(10, 15), shiftResult.Trip!.DepartureTime);

        var shiftedStops = shiftResult.Trip.StopTimes.OrderBy(s => s.Sequence).ToList();
        Assert.Equal(new TimeOnly(10, 15), shiftedStops[0].ArrivalTime);
        Assert.Equal(new TimeOnly(10, 20), shiftedStops[1].ArrivalTime);
    }

    private sealed class FakeTripRepository : ITripRepository
    {
        public Dictionary<Guid, Domain.Trips.Trip> Items { get; } = [];

        public Task<IReadOnlyCollection<Domain.Trips.Trip>> ListByTransitLineAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Domain.Trips.Trip> list = Items.Values
                .Where(t => t.TransitLineId == transitLineId)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<Domain.Trips.Trip?> FindByIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(tripId, out var trip);
            return Task.FromResult(trip);
        }

        public Task AddAsync(Domain.Trips.Trip tripEntity, CancellationToken cancellationToken)
        {
            Items[tripEntity.Id] = tripEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeRoutePathRepository : IRoutePathRepository
    {
        public Dictionary<Guid, RoutePath> Items { get; } = [];

        public Task<IReadOnlyCollection<RoutePath>> ListByTransitLineAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RoutePath> list = Items.Values.Where(r => r.TransitLineId == transitLineId).ToList();
            return Task.FromResult(list);
        }

        public Task<RoutePath?> FindByIdAsync(Guid routePathId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(routePathId, out var path);
            return Task.FromResult(path);
        }

        public Task AddAsync(RoutePath routePathEntity, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeOperatingCalendarRepository : IOperatingCalendarRepository
    {
        public Dictionary<Guid, OperatingCalendar> Items { get; } = [];

        public Task<IReadOnlyCollection<OperatingCalendar>> ListAllAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<OperatingCalendar> list = Items.Values.ToList();
            return Task.FromResult(list);
        }

        public Task<OperatingCalendar?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Items.TryGetValue(id, out var cal);
            return Task.FromResult(cal);
        }

        public Task AddAsync(OperatingCalendar calendarEntity, CancellationToken cancellationToken)
        {
            Items[calendarEntity.Id] = calendarEntity;
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeTransitLineRepository : ITransitLineRepository
    {
        public Dictionary<Guid, TransitLine> Items { get; } = [];

        public Task<TransitLineRepositoryPage> ListAsync(TransitLineRepositoryQuery query, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CodeExistsAsync(string normalizedCode, Guid? excludedTransitLineId, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<TransitLine?> FindByIdAsync(Guid transitLineId, CancellationToken cancellationToken)
        {
            Items.TryGetValue(transitLineId, out var line);
            return Task.FromResult(line);
        }

        public Task AddAsync(TransitLine transitLineEntity, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
