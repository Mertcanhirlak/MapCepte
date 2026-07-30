namespace Transport.Application.Identity;

public sealed record AuditCatalogItem(
    Guid Id,
    string EventType,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    Guid? SubjectUserId,
    string? IpAddress);
