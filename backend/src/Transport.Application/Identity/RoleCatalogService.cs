namespace Transport.Application.Identity;

public sealed class RoleCatalogService(IIdentityRepository identityRepository)
{
    public Task<IReadOnlyCollection<RoleCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return identityRepository.ListRolesAsync(cancellationToken);
    }
}
