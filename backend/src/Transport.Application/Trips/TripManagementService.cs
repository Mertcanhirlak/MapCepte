using Transport.Application.Calendars;
using Transport.Application.RoutePaths;
using Transport.Application.TransitLines;
using Transport.Domain.Trips;

namespace Transport.Application.Trips;

public sealed class TripManagementService(
    ITripRepository tripRepository,
    ITransitLineRepository transitLineRepository,
    IRoutePathRepository routePathRepository,
    IOperatingCalendarRepository calendarRepository,
    ITransitLineAccessPolicy accessPolicy,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<TripCatalogItem>> ListByTransitLineAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty) return [];

        var line = await transitLineRepository.FindByIdAsync(transitLineId, cancellationToken);
        if (line is null || !accessPolicy.CanRead(access, line)) return [];

        var trips = await tripRepository.ListByTransitLineAsync(transitLineId, cancellationToken);
        return trips.Select(ToCatalogItem).ToArray();
    }

    public async Task<TimetableMatrixDto?> GetTimetableMatrixAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty) return null;

        var line = await transitLineRepository.FindByIdAsync(transitLineId, cancellationToken);
        if (line is null || !accessPolicy.CanRead(access, line)) return null;

        var lineStops = line.Stops.OrderBy(s => s.Sequence).ToList();
        var headers = lineStops
            .Select(ls => new TimetableStopHeaderDto(ls.StopId, ls.Sequence))
            .ToArray();

        var trips = await tripRepository.ListByTransitLineAsync(transitLineId, cancellationToken);
        var tripDtos = trips.OrderBy(t => t.DepartureTime).Select(ToCatalogItem).ToArray();

        return new TimetableMatrixDto(transitLineId, headers, tripDtos);
    }

    public async Task<TripResult> CreateTripAsync(
        CreateTripCommand command,
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

        if (string.IsNullOrWhiteSpace(command.TripCode))
        {
            return Invalid("Trip code is required.");
        }

        var line = await transitLineRepository.FindByIdAsync(command.TransitLineId, cancellationToken);
        if (line is null)
        {
            return new TripResult(TripManagementStatus.NotFound, Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, line))
        {
            return new TripResult(TripManagementStatus.Forbidden, Error: "You do not have permission to manage trips for this transit line.");
        }

        var routePath = await routePathRepository.FindByIdAsync(command.RoutePathId, cancellationToken);
        if (routePath is null || routePath.TransitLineId != command.TransitLineId)
        {
            return Invalid("Selected route path is invalid or does not belong to this transit line.");
        }

        var calendar = await calendarRepository.FindByIdAsync(command.OperatingCalendarId, cancellationToken);
        if (calendar is null || !calendar.IsActive)
        {
            return Invalid("Selected operating calendar is inactive or was not found.");
        }

        var now = timeProvider.GetUtcNow();
        var trip = new Trip(
            Guid.NewGuid(),
            command.TransitLineId,
            command.RoutePathId,
            command.OperatingCalendarId,
            command.TripCode,
            command.DepartureTime,
            command.Direction,
            command.Access.UserId,
            now);

        // Generate stop times ETA proportionally based on RoutePathStops and total duration
        var routeStops = routePath.Stops.OrderBy(s => s.Sequence).ToList();
        var totalDuration = routePath.DurationSeconds;

        var stopSnapshots = new List<(Guid stopId, int sequence, double cumulativeDurationSeconds)>();
        if (routeStops.Count > 0)
        {
            for (var i = 0; i < routeStops.Count; i++)
            {
                var progressRatio = routeStops.Count > 1 ? (double)i / (routeStops.Count - 1) : 0;
                var cumulativeSeconds = totalDuration * progressRatio;
                stopSnapshots.Add((routeStops[i].StopId, routeStops[i].Sequence, cumulativeSeconds));
            }
        }

        trip.GenerateStopTimes(stopSnapshots, now);

        await tripRepository.AddAsync(trip, cancellationToken);
        await tripRepository.SaveChangesAsync(cancellationToken);

        return new TripResult(TripManagementStatus.Success, ToCatalogItem(trip));
    }

    public async Task<TripResult> ShiftTripTimeAsync(
        ShiftTripTimeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TripId == Guid.Empty)
        {
            return Invalid("Trip id cannot be empty.");
        }

        var trip = await tripRepository.FindByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return new TripResult(TripManagementStatus.NotFound, Error: "Trip was not found.");
        }

        var line = await transitLineRepository.FindByIdAsync(trip.TransitLineId, cancellationToken);
        if (line is null || !accessPolicy.CanManage(command.Access, line))
        {
            return new TripResult(TripManagementStatus.Forbidden, Error: "You do not have permission to manage this trip.");
        }

        var now = timeProvider.GetUtcNow();
        trip.ShiftDepartureTime(TimeSpan.FromMinutes(command.MinutesOffset), now);

        await tripRepository.SaveChangesAsync(cancellationToken);
        return new TripResult(TripManagementStatus.Success, ToCatalogItem(trip));
    }

    private static TripResult Invalid(string error) =>
        new(TripManagementStatus.InvalidInput, Error: error);

    private static TripCatalogItem ToCatalogItem(Trip trip) =>
        new(
            trip.Id,
            trip.TransitLineId,
            trip.RoutePathId,
            trip.OperatingCalendarId,
            trip.TripCode,
            trip.DepartureTime,
            trip.Direction.ToString(),
            trip.IsPublished,
            trip.StopTimes
                .OrderBy(st => st.Sequence)
                .Select(st => new TripStopTimeDto(st.Id, st.StopId, st.Sequence, st.ArrivalTime, st.DepartureTime))
                .ToArray());
}
