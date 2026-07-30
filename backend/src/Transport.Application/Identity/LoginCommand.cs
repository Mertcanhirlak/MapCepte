namespace Transport.Application.Identity;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null);
