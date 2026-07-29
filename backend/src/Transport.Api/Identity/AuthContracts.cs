namespace Transport.Api.Identity;

public sealed record LoginRequest(string Email, string Password);

public sealed record CsrfTokenResponse(string Token);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
