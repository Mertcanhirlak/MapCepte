namespace Transport.Application.TransitLines;

public sealed record CreateTransitLineCommand(
    TransitLineAccessContext Access,
    string Name,
    string Code,
    string? Description,
    string Color);

public sealed record UpdateTransitLineCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    string Name,
    string Code,
    string? Description,
    string Color,
    long ExpectedVersion);

public sealed record ArchiveTransitLineCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    long ExpectedVersion);

public sealed record AddStopToLineCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    Guid StopId,
    long ExpectedVersion);

public sealed record RemoveStopFromLineCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    Guid StopId,
    long ExpectedVersion);

public sealed record ReorderLineStopsCommand(
    TransitLineAccessContext Access,
    Guid TransitLineId,
    IReadOnlyList<Guid> OrderedStopIds,
    long ExpectedVersion);

public sealed record TransitLineListQuery(
    TransitLineAccessContext Access,
    string? Search,
    int Page,
    int PageSize);

public sealed record TransitLineRepositoryQuery(
    Guid ActorUserId,
    TransitLineVisibilityScope Scope,
    string? Search,
    int Page,
    int PageSize);

public sealed record TransitLineRepositoryPage(
    IReadOnlyCollection<Transport.Domain.TransitLines.TransitLine> Items,
    int TotalCount);

public sealed record TransitLineCatalogPage(
    IReadOnlyCollection<TransitLineCatalogItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record TransitLineListResult(
    TransitLineManagementStatus Status,
    TransitLineCatalogPage? Page = null,
    string? Error = null);

public sealed record TransitLineStopItem(
    Guid LineStopId,
    Guid StopId,
    string StopName,
    string? StopCode,
    string StopColor,
    double Longitude,
    double Latitude,
    int Sequence);

public sealed record TransitLineStopsResult(
    TransitLineManagementStatus Status,
    IReadOnlyCollection<TransitLineStopItem>? Stops = null,
    string? Error = null);

public enum TransitLineManagementStatus
{
    Success = 0,
    InvalidInput = 1,
    DuplicateCode = 2,
    NotFound = 3,
    Forbidden = 4,
    Conflict = 5,
    AlreadyArchived = 6,
    StopNotFound = 7,
    StopAlreadyInLine = 8,
    StopNotInLine = 9,
}

public sealed record TransitLineManagementResult(
    TransitLineManagementStatus Status,
    TransitLineCatalogItem? TransitLine = null,
    string? Error = null);

public enum TransitLineVisibilityScope
{
    All = 0,
    Owned = 1,
    Published = 2,
}
