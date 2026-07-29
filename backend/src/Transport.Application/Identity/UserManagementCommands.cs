namespace Transport.Application.Identity;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyCollection<string> Roles,
    bool AllowWeakPassword = false);

public sealed record UpdateUserRolesCommand(
    Guid ActorUserId,
    Guid UserId,
    IReadOnlyCollection<string> Roles);
