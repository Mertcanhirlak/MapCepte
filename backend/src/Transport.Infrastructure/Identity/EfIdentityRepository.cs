using Microsoft.EntityFrameworkCore;
using Transport.Application.Identity;
using Transport.Domain.Identity;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Identity;

public sealed class EfIdentityRepository(TransportDbContext dbContext)
    : IIdentityRepository
{
    public Task<bool> HasAdminAsync(CancellationToken cancellationToken)
    {
        var normalizedAdminRole = SystemRoleNames.Admin.ToUpperInvariant();

        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.UserRoles.Any(userRole =>
                    userRole.Role.NormalizedName == normalizedAdminRole),
                cancellationToken);
    }

    public Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public Task<User?> FindUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);
    }

    public async Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                    .ThenInclude(role => role.RolePermissions)
                        .ThenInclude(rolePermission =>
                            rolePermission.Permission)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var permissions = user.UserRoles
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Select(rolePermission => rolePermission.Permission.Code)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new UserAuthenticationData(user, roles, permissions);
    }

    public Task<Role?> FindRoleByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(
                role => role.NormalizedName == normalizedName,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleCatalogItem>> ListRolesAsync(
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles
            .AsNoTracking()
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .AsSplitQuery()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleCatalogItem(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystem,
                role.RolePermissions
                    .Select(rolePermission =>
                        rolePermission.Permission.Code)
                    .Order(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<UserCatalogItem>> ListUsersAsync(
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .AsSplitQuery()
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToListAsync(cancellationToken);

        return users
            .Select(user => new UserCatalogItem(
                user.Id,
                user.Email,
                user.DisplayName,
                user.IsActive,
                user.CreatedAtUtc,
                user.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
    }

    public async Task AddUserAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
