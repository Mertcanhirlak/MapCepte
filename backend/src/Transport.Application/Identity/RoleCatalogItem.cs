namespace Transport.Application.Identity;

public sealed record RoleCatalogItem(
    Guid Id,
    string Name,
    string Description,
    bool IsSystem,
    IReadOnlyCollection<string> Permissions);
