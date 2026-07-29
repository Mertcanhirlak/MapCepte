using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transport.Application.Identity;
using Transport.Domain.Identity;
using Transport.Infrastructure.Identity;
using Transport.Infrastructure.Persistence;

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
        services.AddScoped<AdminBootstrapService>();
        services.AddScoped<LoginService>();
        services.AddScoped<RoleCatalogService>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
