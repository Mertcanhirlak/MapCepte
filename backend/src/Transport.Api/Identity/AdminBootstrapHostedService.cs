using Microsoft.Extensions.Options;
using Transport.Application.Identity;

namespace Transport.Api.Identity;

public sealed partial class AdminBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdminOptions> options,
    ILogger<AdminBootstrapHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var bootstrapService =
            scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();

        var result = await bootstrapService.BootstrapAsync(
            new BootstrapAdminCommand(
                options.Value.Email,
                options.Value.DisplayName,
                options.Value.Password),
            cancellationToken);

        if (result.Status == BootstrapAdminStatus.Created)
        {
            LogAdminCreated(logger);
        }
        else
        {
            LogAdminAlreadyExists(logger);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Initial Admin account was created successfully.")]
    private static partial void LogAdminCreated(ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Admin bootstrap skipped because an Admin account already exists.")]
    private static partial void LogAdminAlreadyExists(ILogger logger);
}
