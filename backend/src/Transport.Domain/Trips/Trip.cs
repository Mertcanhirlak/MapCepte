using Transport.Domain.RoutePaths;

namespace Transport.Domain.Trips;

public sealed class Trip
{
    private readonly List<TripStopTime> stopTimes = [];

    private Trip()
    {
    }

    public Trip(
        Guid id,
        Guid transitLineId,
        Guid routePathId,
        Guid operatingCalendarId,
        string tripCode,
        TimeOnly departureTime,
        RoutePathDirection direction,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Trip id cannot be empty.", nameof(id));
        }

        if (transitLineId == Guid.Empty)
        {
            throw new ArgumentException("Transit line id cannot be empty.", nameof(transitLineId));
        }

        if (routePathId == Guid.Empty)
        {
            throw new ArgumentException("Route path id cannot be empty.", nameof(routePathId));
        }

        if (operatingCalendarId == Guid.Empty)
        {
            throw new ArgumentException("Operating calendar id cannot be empty.", nameof(operatingCalendarId));
        }

        if (string.IsNullOrWhiteSpace(tripCode))
        {
            throw new ArgumentException("Trip code cannot be empty.", nameof(tripCode));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user id cannot be empty.", nameof(createdByUserId));
        }

        Id = id;
        TransitLineId = transitLineId;
        RoutePathId = routePathId;
        OperatingCalendarId = operatingCalendarId;
        TripCode = tripCode.Trim();
        DepartureTime = departureTime;
        Direction = direction;
        IsPublished = true;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TransitLineId { get; private set; }

    public Guid RoutePathId { get; private set; }

    public Guid OperatingCalendarId { get; private set; }

    public string TripCode { get; private set; } = string.Empty;

    public TimeOnly DepartureTime { get; private set; }

    public RoutePathDirection Direction { get; private set; }

    public bool IsPublished { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<TripStopTime> StopTimes => stopTimes;

    public void GenerateStopTimes(
        IReadOnlyList<(Guid stopId, int sequence, double cumulativeDurationSeconds)> stopsWithCumulativeSeconds,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(stopsWithCumulativeSeconds);

        stopTimes.Clear();
        foreach (var (stopId, sequence, cumulativeSeconds) in stopsWithCumulativeSeconds)
        {
            var etaTime = DepartureTime.Add(TimeSpan.FromSeconds(cumulativeSeconds));
            stopTimes.Add(new TripStopTime(
                Guid.NewGuid(),
                Id,
                stopId,
                sequence,
                etaTime,
                etaTime));
        }

        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    public void ShiftDepartureTime(TimeSpan offset, DateTimeOffset updatedAtUtc)
    {
        DepartureTime = DepartureTime.Add(offset);
        var oldTimes = stopTimes.ToList();
        stopTimes.Clear();

        foreach (var st in oldTimes)
        {
            stopTimes.Add(new TripStopTime(
                st.Id,
                st.TripId,
                st.StopId,
                st.Sequence,
                st.ArrivalTime.Add(offset),
                st.DepartureTime.Add(offset)));
        }

        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }
}
