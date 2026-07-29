namespace Transport.Application.Identity;

public sealed record BootstrapAdminCommand(
    string Email,
    string DisplayName,
    string Password,
    bool AllowWeakPassword = false);
