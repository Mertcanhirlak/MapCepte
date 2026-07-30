namespace Transport.Api.Authorization;

public sealed record RoleCatalogResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystem,
    IReadOnlyCollection<string> Permissions);

public sealed record UserCatalogResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyCollection<string> Roles);

public sealed record UpdateUserRolesRequest(
    IReadOnlyCollection<string> Roles);

public sealed record AuditCatalogResponse(
    Guid Id,
    string EventType,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    Guid? SubjectUserId,
    string? IpAddress);
