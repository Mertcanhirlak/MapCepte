namespace Transport.Application.TransitLines;

public sealed record TransitLineCatalogItem(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Color,
    string Status,
    Guid OwnerUserId,
    Guid CreatedByUserId,
    Guid UpdatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version,
    int StopCount);
