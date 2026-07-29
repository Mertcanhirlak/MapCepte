namespace Transport.Application.Identity;

public enum LoginStatus
{
    Success = 0,
    InvalidCredentials = 1,
}

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record LoginResult(
    LoginStatus Status,
    AuthenticatedUser? User);
