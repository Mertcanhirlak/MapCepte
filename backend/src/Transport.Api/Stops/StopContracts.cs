namespace Transport.Api.Stops;

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
