using Microsoft.EntityFrameworkCore;
using Transport.Domain.Identity;
using Transport.Infrastructure.Persistence.Seeding;

namespace Transport.Infrastructure.Persistence;

public sealed class TransportDbContext(DbContextOptions<TransportDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransportDbContext).Assembly);
        IdentitySeedData.Apply(modelBuilder);
    }
}
