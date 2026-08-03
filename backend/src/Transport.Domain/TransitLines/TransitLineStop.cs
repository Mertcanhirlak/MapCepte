namespace Transport.Domain.TransitLines;

public sealed class TransitLineStop
{
    private TransitLineStop()
    {
    }

    internal TransitLineStop(
        Guid id,
        Guid transitLineId,
        Guid stopId,
        int sequence,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Transit line stop id cannot be empty.",
                nameof(id));
        }

        if (transitLineId == Guid.Empty)
        {
            throw new ArgumentException(
                "Transit line id cannot be empty.",
                nameof(transitLineId));
        }

        if (stopId == Guid.Empty)
        {
            throw new ArgumentException(
                "Stop id cannot be empty.",
                nameof(stopId));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "Sequence must start at one.");
        }

        Id = id;
        TransitLineId = transitLineId;
        StopId = stopId;
        Sequence = sequence;
        BoardingAllowed = true;
        AlightingAllowed = true;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid TransitLineId { get; private set; }

    public Guid StopId { get; private set; }

    public int Sequence { get; private set; }

    public bool BoardingAllowed { get; private set; }

    public bool AlightingAllowed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal void MoveTo(int sequence)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                "Sequence must start at one.");
        }

        Sequence = sequence;
    }
}
