namespace Transport.Application.Stops;

public sealed record StopCatalogItem(
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
