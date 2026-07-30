namespace Transport.Domain.Identity;

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(
        Guid id,
        string eventType,
        string outcome,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? subjectUserId = null,
        string? ipAddress = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Audit entry id cannot be empty.",
                nameof(id));
        }

        Id = id;
        EventType = RequireText(eventType, nameof(eventType));
        Outcome = RequireText(outcome, nameof(outcome));
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        IpAddress = string.IsNullOrWhiteSpace(ipAddress)
            ? null
            : ipAddress.Trim();
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? SubjectUserId { get; private set; }

    public string? IpAddress { get; private set; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
