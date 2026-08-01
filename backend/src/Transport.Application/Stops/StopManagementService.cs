using System.Buffers;
using Transport.Domain.Stops;

namespace Transport.Application.Stops;

public sealed class StopManagementService(
    IStopRepository stopRepository,
    IStopAccessPolicy accessPolicy,
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
                excludedStopId: null,
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

    public async Task<StopManagementResult> UpdateAsync(
        UpdateStopCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationError = ValidateUpdate(command);
        if (validationError is not null)
        {
            return Invalid(validationError);
        }

        var stopEntity = await stopRepository.FindByIdAsync(
            command.StopId,
            cancellationToken);
        var accessError = ValidateManagedStop(command.Access, stopEntity);
        if (accessError is not null)
        {
            return accessError;
        }

        if (stopEntity!.Version != command.ExpectedVersion)
        {
            return Conflict();
        }

        var normalizedCode = command.Code?.Trim().ToUpperInvariant();
        if (normalizedCode is not null
            && await stopRepository.CodeExistsAsync(
                normalizedCode,
                command.StopId,
                cancellationToken))
        {
            return new StopManagementResult(
                StopManagementStatus.DuplicateCode,
                Error: "A stop with this code already exists.");
        }

        stopEntity.UpdateDetails(
            command.Name,
            command.Code,
            command.Description,
            command.Color,
            command.Longitude,
            command.Latitude,
            command.Access.UserId,
            timeProvider.GetUtcNow());

        return await SaveUpdatedAsync(stopEntity, cancellationToken);
    }

    public async Task<StopManagementResult> ArchiveAsync(
        ArchiveStopCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.StopId == Guid.Empty || command.ExpectedVersion <= 0)
        {
            return Invalid("A valid stop id and version are required.");
        }

        var stopEntity = await stopRepository.FindByIdAsync(
            command.StopId,
            cancellationToken);
        var accessError = ValidateManagedStop(command.Access, stopEntity);
        if (accessError is not null)
        {
            return accessError;
        }

        if (stopEntity!.Status == StopStatus.Archived)
        {
            return new StopManagementResult(
                StopManagementStatus.AlreadyArchived,
                Error: "Stop is already archived.");
        }

        if (stopEntity.Version != command.ExpectedVersion)
        {
            return Conflict();
        }

        stopEntity.Archive(
            command.Access.UserId,
            timeProvider.GetUtcNow());

        return await SaveUpdatedAsync(stopEntity, cancellationToken);
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

    private static string? ValidateUpdate(UpdateStopCommand command)
    {
        if (command.Access is null
            || command.StopId == Guid.Empty
            || command.ExpectedVersion <= 0)
        {
            return "A valid actor, stop id and version are required.";
        }

        return Validate(new CreateStopCommand(
            command.Access.UserId,
            command.Name,
            command.Code,
            command.Description,
            command.Color,
            command.Longitude,
            command.Latitude));
    }

    private StopManagementResult? ValidateManagedStop(
        StopAccessContext access,
        Stop? stopEntity)
    {
        if (stopEntity is null)
        {
            return new StopManagementResult(
                StopManagementStatus.NotFound,
                Error: "Stop was not found.");
        }

        if (!accessPolicy.CanManage(access, stopEntity))
        {
            return new StopManagementResult(
                StopManagementStatus.Forbidden,
                Error: "You cannot manage this stop.");
        }

        return null;
    }

    private async Task<StopManagementResult> SaveUpdatedAsync(
        Stop stopEntity,
        CancellationToken cancellationToken)
    {
        if (!await stopRepository.SaveChangesAsync(cancellationToken))
        {
            return Conflict();
        }

        return new StopManagementResult(
            StopManagementStatus.Success,
            ToCatalogItem(stopEntity));
    }

    private static StopManagementResult Conflict()
    {
        return new StopManagementResult(
            StopManagementStatus.Conflict,
            Error: "Stop changed after it was loaded. Refresh and try again.");
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
            stop.CreatedAtUtc,
            stop.UpdatedAtUtc,
            stop.Version);
    }
}
