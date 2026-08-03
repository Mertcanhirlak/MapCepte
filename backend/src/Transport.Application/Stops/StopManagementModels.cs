namespace Transport.Application.Stops;

public sealed record CreateStopCommand(
    Guid ActorUserId,
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude);

public sealed record UpdateStopCommand(
    StopAccessContext Access,
    Guid StopId,
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude,
    long ExpectedVersion);

public sealed record ArchiveStopCommand(
    StopAccessContext Access,
    Guid StopId,
    long ExpectedVersion);

public sealed record StopBounds(
    double MinLongitude,
    double MinLatitude,
    double MaxLongitude,
    double MaxLatitude);

public sealed record StopListQuery(
    StopAccessContext Access,
    string? Search,
    int Page,
    int PageSize,
    StopBounds? Bounds);

public sealed record StopRepositoryQuery(
    Guid ActorUserId,
    StopVisibilityScope Scope,
    string? Search,
    int Page,
    int PageSize,
    StopBounds? Bounds);

public sealed record StopRepositoryPage(
    IReadOnlyCollection<Transport.Domain.Stops.Stop> Items,
    int TotalCount);

public sealed record StopCatalogPage(
    IReadOnlyCollection<StopCatalogItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record StopListResult(
    StopManagementStatus Status,
    StopCatalogPage? Page = null,
    string? Error = null);

public enum StopManagementStatus
{
    Success = 0,
    InvalidInput = 1,
    DuplicateCode = 2,
    NotFound = 3,
    Forbidden = 4,
    Conflict = 5,
    AlreadyArchived = 6,
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
