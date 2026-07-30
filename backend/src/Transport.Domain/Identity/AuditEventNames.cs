namespace Transport.Domain.Identity;

public static class AuditEventNames
{
    public const string Login = "auth.login";
    public const string UserCreated = "admin.user.created";
    public const string UserRolesUpdated = "admin.user.roles_updated";
}

public static class AuditOutcomes
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string LockedOut = "locked_out";
}
