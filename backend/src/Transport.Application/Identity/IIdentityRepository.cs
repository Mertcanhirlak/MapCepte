using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public interface IIdentityRepository
{
    Task<bool> HasAdminAsync(CancellationToken cancellationToken);

    Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<Role?> FindRoleByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleCatalogItem>> ListRolesAsync(
        CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
