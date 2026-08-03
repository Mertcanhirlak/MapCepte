namespace Transport.Domain.Calendars;

[Flags]
public enum DaysOfWeek
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    Everyday = Weekdays | Weekend,
}

public sealed class OperatingCalendar
{
    private OperatingCalendar()
    {
    }

    public OperatingCalendar(
        Guid id,
        string name,
        DaysOfWeek daysOfWeek,
        bool isActive,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Calendar id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Calendar name cannot be empty.", nameof(name));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user id cannot be empty.", nameof(createdByUserId));
        }

        Id = id;
        Name = name.Trim();
        DaysOfWeek = daysOfWeek;
        IsActive = isActive;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DaysOfWeek DaysOfWeek { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string name, DaysOfWeek daysOfWeek, bool isActive, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Calendar name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        DaysOfWeek = daysOfWeek;
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }
}
