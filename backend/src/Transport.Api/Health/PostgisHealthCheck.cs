using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Transport.Infrastructure.Persistence;

namespace Transport.Api.Health;

public sealed partial class PostgisHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<PostgisHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransportDbContext>();

        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);

            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT PostGIS_Version();";

            var version = await command.ExecuteScalarAsync(cancellationToken);

            return version is null
                ? HealthCheckResult.Unhealthy("PostgreSQL is reachable but PostGIS is unavailable.")
                : HealthCheckResult.Healthy(
                    "PostgreSQL and PostGIS are ready.",
                    new Dictionary<string, object>
                    {
                        ["postgisVersion"] = version.ToString() ?? "unknown",
                    });
        }
        catch (Exception exception)
        {
            LogReadinessFailure(logger, exception);
            return HealthCheckResult.Unhealthy(
                "PostgreSQL/PostGIS is not ready.",
                exception);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "PostGIS readiness check failed.")]
    private static partial void LogReadinessFailure(
        ILogger logger,
        Exception exception);
}
