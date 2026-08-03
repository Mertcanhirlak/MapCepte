namespace Transport.Api.Stops;

public sealed record StopListRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    double? MinLongitude = null,
    double? MinLatitude = null,
    double? MaxLongitude = null,
    double? MaxLatitude = null);

public sealed record CreateStopRequest(
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude);

public sealed record UpdateStopRequest(
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude,
    long Version);

public sealed record ArchiveStopRequest(long Version);

public sealed record StopResponse(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    string Color,
    double Longitude,
    double Latitude,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record StopPageResponse(
    IReadOnlyCollection<StopResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
