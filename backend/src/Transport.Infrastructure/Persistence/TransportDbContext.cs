using Microsoft.EntityFrameworkCore;
using Transport.Domain.Calendars;
using Transport.Domain.Identity;
using Transport.Domain.RoutePaths;
using Transport.Domain.Stops;
using Transport.Domain.TransitLines;
using Transport.Domain.Trips;
using Transport.Domain.Vehicles;
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

    public DbSet<Stop> Stops => Set<Stop>();

    public DbSet<TransitLine> TransitLines => Set<TransitLine>();

    public DbSet<TransitLineStop> TransitLineStops => Set<TransitLineStop>();

    public DbSet<RoutePath> RoutePaths => Set<RoutePath>();

    public DbSet<RoutePathStop> RoutePathStops => Set<RoutePathStop>();

    public DbSet<OperatingCalendar> OperatingCalendars => Set<OperatingCalendar>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<TripStopTime> TripStopTimes => Set<TripStopTime>();

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransportDbContext).Assembly);
        IdentitySeedData.Apply(modelBuilder);
    }
}
