using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transport.Application.Calendars;
using Transport.Application.Identity;
using Transport.Application.RoutePaths;
using Transport.Application.Routing;
using Transport.Application.Stops;
using Transport.Application.TransitLines;
using Transport.Application.Trips;
using Transport.Application.Vehicles;
using Transport.Domain.Identity;
using Transport.Infrastructure.Calendars;
using Transport.Infrastructure.Identity;
using Transport.Infrastructure.Persistence;
using Transport.Infrastructure.RoutePaths;
using Transport.Infrastructure.Routing;
using Transport.Infrastructure.Stops;
using Transport.Infrastructure.TransitLines;
using Transport.Infrastructure.Trips;
using Transport.Infrastructure.Vehicles;

namespace Transport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TransportDb")
            ?? throw new InvalidOperationException(
                "Connection string 'TransportDb' is not configured.");

        services.AddDbContext<TransportDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.UseNetTopologySuite()));

        services.AddOptions<PasswordHasherOptions>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHashService, AspNetPasswordHashService>();
        services.AddScoped<IIdentityRepository, EfIdentityRepository>();
        services.AddScoped<IAuditStore, EfAuditStore>();
        services.AddScoped<AdminBootstrapService>();
        services.AddScoped<LoginService>();
        services.AddScoped<RoleCatalogService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<AuditCatalogService>();
        services.AddScoped<IStopRepository, EfStopRepository>();
        services.AddSingleton<IStopAccessPolicy, StopAccessPolicy>();
        services.AddScoped<StopManagementService>();
        services.AddScoped<ITransitLineRepository, EfTransitLineRepository>();
        services.AddSingleton<ITransitLineAccessPolicy, TransitLineAccessPolicy>();
        services.AddScoped<TransitLineManagementService>();
        services.AddSingleton<IRoutingEngine, MockRoutingEngine>();
        services.AddScoped<IRoutePathRepository, EfRoutePathRepository>();
        services.AddScoped<RoutePathManagementService>();
        services.AddScoped<IOperatingCalendarRepository, EfOperatingCalendarRepository>();
        services.AddScoped<OperatingCalendarManagementService>();
        services.AddScoped<ITripRepository, EfTripRepository>();
        services.AddScoped<TripManagementService>();
        services.AddScoped<IVehiclePositionRepository, EfVehiclePositionRepository>();
        services.AddScoped<VehicleTrackingService>();
        var maximumFailedAttempts = configuration.GetValue<int?>(
                "IdentitySecurity:MaximumFailedLoginAttempts")
            ?? 5;
        var lockoutMinutes = configuration.GetValue<int?>(
                "IdentitySecurity:LockoutMinutes")
            ?? 15;

        if (maximumFailedAttempts <= 0 || lockoutMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Identity lockout settings must be positive.");
        }

        services.AddSingleton(
            new LoginSecurityPolicy(
                maximumFailedAttempts,
                TimeSpan.FromMinutes(lockoutMinutes)));
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
