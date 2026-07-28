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

    public Task<Role?> FindRoleByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(
                role => role.NormalizedName == normalizedName,
                cancellationToken);
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
