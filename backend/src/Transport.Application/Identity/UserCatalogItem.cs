namespace Transport.Application.Identity;

public sealed record UserCatalogItem(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> Roles);
