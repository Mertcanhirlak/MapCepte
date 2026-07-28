namespace Transport.Application.Identity;

public enum BootstrapAdminStatus
{
    Created = 0,
    AlreadyConfigured = 1,
}

public sealed record BootstrapAdminResult(
    BootstrapAdminStatus Status,
    Guid? UserId);
