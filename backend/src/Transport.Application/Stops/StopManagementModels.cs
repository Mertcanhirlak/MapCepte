namespace Transport.Application.Stops;

public sealed record CreateStopCommand(
    Guid ActorUserId,
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude);

public enum StopManagementStatus
{
    Success = 0,
    InvalidInput = 1,
    DuplicateCode = 2,
}

public sealed record StopManagementResult(
    StopManagementStatus Status,
    StopCatalogItem? Stop = null,
    string? Error = null);

public enum StopVisibilityScope
{
    All = 0,
    Owned = 1,
    Published = 2,
}
