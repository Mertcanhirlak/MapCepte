using System.Buffers;
using Transport.Domain.Stops;

namespace Transport.Application.Stops;

public sealed class StopManagementService(
    IStopRepository stopRepository,
    TimeProvider timeProvider)
{
    private static readonly SearchValues<char> HexadecimalCharacters =
        SearchValues.Create("0123456789abcdefABCDEF");

    public async Task<IReadOnlyCollection<StopCatalogItem>> ListAsync(
        StopAccessContext access,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (access.UserId == Guid.Empty)
        {
            return [];
        }

        var scope = access.IsAdmin
            ? StopVisibilityScope.All
            : access.IsOperator
                ? StopVisibilityScope.Owned
                : StopVisibilityScope.Published;

        var stops = await stopRepository.ListAsync(
            access.UserId,
            scope,
            cancellationToken);

        return stops.Select(ToCatalogItem).ToArray();
    }

    public async Task<StopManagementResult> CreateAsync(
        CreateStopCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var error = Validate(command);
        if (error is not null)
        {
            return Invalid(error);
        }

        var normalizedCode = command.Code?.Trim().ToUpperInvariant();
        if (normalizedCode is not null
            && await stopRepository.CodeExistsAsync(
                normalizedCode,
                cancellationToken))
        {
            return new StopManagementResult(
                StopManagementStatus.DuplicateCode,
                Error: "A stop with this code already exists.");
        }

        var stop = new Stop(
            Guid.NewGuid(),
            command.Name,
            command.Code,
            command.Description,
            command.Color,
            command.Longitude,
            command.Latitude,
            command.ActorUserId,
            timeProvider.GetUtcNow());

        await stopRepository.AddAsync(stop, cancellationToken);
        await stopRepository.SaveChangesAsync(cancellationToken);

        return new StopManagementResult(
            StopManagementStatus.Success,
            ToCatalogItem(stop));
    }

    private static string? Validate(CreateStopCommand command)
    {
        if (command.ActorUserId == Guid.Empty)
        {
            return "A valid actor is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Name)
            || command.Name.Trim().Length > 160)
        {
            return "Stop name must contain at most 160 characters.";
        }

        if (command.Code?.Trim().Length > 40)
        {
            return "Stop code must contain at most 40 characters.";
        }

        if (command.Description?.Trim().Length > 1000)
        {
            return "Description must contain at most 1000 characters.";
        }

        if (string.IsNullOrWhiteSpace(command.Color)
            || command.Color.Length != 7
            || command.Color[0] != '#'
            || command.Color.AsSpan(1).ContainsAnyExcept(
                HexadecimalCharacters))
        {
            return "Color must use #RRGGBB format.";
        }

        if (!double.IsFinite(command.Longitude)
            || command.Longitude is < -180 or > 180)
        {
            return "Longitude must be between -180 and 180.";
        }

        if (!double.IsFinite(command.Latitude)
            || command.Latitude is < -90 or > 90)
        {
            return "Latitude must be between -90 and 90.";
        }

        return null;
    }

    private static StopManagementResult Invalid(string error)
    {
        return new StopManagementResult(
            StopManagementStatus.InvalidInput,
            Error: error);
    }

    private static StopCatalogItem ToCatalogItem(Stop stop)
    {
        return new StopCatalogItem(
            stop.Id,
            stop.Name,
            stop.Code,
            stop.Description,
            stop.Color,
            stop.Location.X,
            stop.Location.Y,
            stop.Status.ToString(),
            stop.CreatedByUserId,
            stop.CreatedAtUtc);
    }
}
