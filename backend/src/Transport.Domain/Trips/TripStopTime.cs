namespace Transport.Domain.Trips;

public sealed class TripStopTime
{
    private TripStopTime()
    {
    }

    internal TripStopTime(
        Guid id,
        Guid tripId,
        Guid stopId,
        int sequence,
        TimeOnly arrivalTime,
        TimeOnly departureTime)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Trip stop time id cannot be empty.", nameof(id));
        }

        if (tripId == Guid.Empty)
        {
            throw new ArgumentException("Trip id cannot be empty.", nameof(tripId));
        }

        if (stopId == Guid.Empty)
        {
            throw new ArgumentException("Stop id cannot be empty.", nameof(stopId));
        }

        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be greater than zero.");
        }

        Id = id;
        TripId = tripId;
        StopId = stopId;
        Sequence = sequence;
        ArrivalTime = arrivalTime;
        DepartureTime = departureTime;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    public Guid StopId { get; private set; }

    public int Sequence { get; private set; }

    public TimeOnly ArrivalTime { get; private set; }

    public TimeOnly DepartureTime { get; private set; }
}
