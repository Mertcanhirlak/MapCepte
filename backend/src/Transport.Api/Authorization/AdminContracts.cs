namespace Transport.Api.Authorization;

public sealed record RoleCatalogResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystem,
    IReadOnlyCollection<string> Permissions);
