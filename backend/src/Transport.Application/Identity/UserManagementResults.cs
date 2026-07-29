namespace Transport.Application.Identity;

public enum UserManagementStatus
{
    Success,
    InvalidInput,
    DuplicateEmail,
    UserNotFound,
    UnknownRole,
    SelfRoleChangeForbidden,
}

public sealed record UserManagementResult(
    UserManagementStatus Status,
    UserCatalogItem? User = null,
    string? Error = null);
