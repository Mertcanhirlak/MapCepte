using System.Buffers;
using Transport.Application.Stops;
using Transport.Domain.Stops;
using Transport.Domain.TransitLines;

namespace Transport.Application.TransitLines;

public sealed class TransitLineManagementService(
    ITransitLineRepository transitLineRepository,
    IStopRepository stopRepository,
    ITransitLineAccessPolicy accessPolicy,
    TimeProvider timeProvider)
{
    private static readonly SearchValues<char> HexadecimalCharacters =
        SearchValues.Create("0123456789abcdefABCDEF");

    public async Task<TransitLineListResult> ListAsync(
        TransitLineListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var validationError = ValidateList(query);
        if (validationError is not null)
        {
            return new TransitLineListResult(
                TransitLineManagementStatus.InvalidInput,
                Error: validationError);
        }

        var scope = query.Access.IsAdmin
            ? TransitLineVisibilityScope.All
            : query.Access.IsOperator
                ? TransitLineVisibilityScope.Owned
                : TransitLineVisibilityScope.Published;

        var repositoryPage = await transitLineRepository.ListAsync(
            new TransitLineRepositoryQuery(
                query.Access.UserId,
                scope,
                string.IsNullOrWhiteSpace(query.Search)
                    ? null
                    : query.Search.Trim(),
                query.Page,
                query.PageSize),
            cancellationToken);

        var totalPages = repositoryPage.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(repositoryPage.TotalCount / (double)query.PageSize);

        return new TransitLineListResult(
            TransitLineManagementStatus.Success,
            new TransitLineCatalogPage(
                repositoryPage.Items.Select(ToCatalogItem).ToArray(),
                query.Page,
                query.PageSize,
                repositoryPage.TotalCount,
                totalPages));
    }

    public async Task<TransitLineManagementResult> GetByIdAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty)
        {
            return Invalid("Transit line id cannot be empty.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            transitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanRead(access, transitLine))
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to view this transit line.");
        }

        return new TransitLineManagementResult(
            TransitLineManagementStatus.Success,
            ToCatalogItem(transitLine));
    }

    public async Task<TransitLineStopsResult> GetStopsAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.InvalidInput,
                Error: "Transit line id cannot be empty.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            transitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanRead(access, transitLine))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to view this transit line.");
        }

        var lineStops = transitLine.Stops.OrderBy(s => s.Sequence).ToList();
        var stopIds = lineStops.Select(s => s.StopId).ToList();
        var stopEntities = await stopRepository.FindByIdsAsync(
            stopIds,
            cancellationToken);

        var resultStops = lineStops
            .Where(ls => stopEntities.ContainsKey(ls.StopId))
            .Select(ls =>
            {
                var stop = stopEntities[ls.StopId];
                return new TransitLineStopItem(
                    ls.Id,
                    ls.StopId,
                    stop.Name,
                    stop.Code,
                    stop.Color,
                    stop.Location.X,
                    stop.Location.Y,
                    ls.Sequence);
            })
            .ToArray();

        return new TransitLineStopsResult(
            TransitLineManagementStatus.Success,
            Stops: resultStops);
    }

    public async Task<TransitLineManagementResult> CreateAsync(
        CreateTransitLineCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var error = ValidateCreate(command);
        if (error is not null)
        {
            return Invalid(error);
        }

        if (!command.Access.IsAdmin && !command.Access.IsOperator)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Forbidden,
                Error: "Only admins and operators can create transit lines.");
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        if (await transitLineRepository.CodeExistsAsync(
                normalizedCode,
                excludedTransitLineId: null,
                cancellationToken))
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.DuplicateCode,
                Error: "A transit line with the specified code already exists.");
        }

        var now = timeProvider.GetUtcNow();
        var transitLine = new TransitLine(
            Guid.NewGuid(),
            command.Name,
            command.Code,
            command.Description,
            command.Color,
            ownerUserId: command.Access.UserId,
            createdByUserId: command.Access.UserId,
            createdAtUtc: now);

        await transitLineRepository.AddAsync(transitLine, cancellationToken);
        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return new TransitLineManagementResult(
            TransitLineManagementStatus.Success,
            ToCatalogItem(transitLine));
    }

    public async Task<TransitLineManagementResult> UpdateAsync(
        UpdateTransitLineCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var error = ValidateUpdate(command);
        if (error is not null)
        {
            return Invalid(error);
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            command.TransitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to manage this transit line.");
        }

        if (transitLine.Status == TransitLineStatus.Archived)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.AlreadyArchived,
                Error: "Archived transit lines cannot be modified.");
        }

        if (transitLine.Version != command.ExpectedVersion)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Conflict,
                Error: "Transit line was modified by another operation.");
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        if (await transitLineRepository.CodeExistsAsync(
                normalizedCode,
                excludedTransitLineId: command.TransitLineId,
                cancellationToken))
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.DuplicateCode,
                Error: "A transit line with the specified code already exists.");
        }

        var now = timeProvider.GetUtcNow();
        transitLine.UpdateDetails(
            command.Name,
            command.Code,
            command.Description,
            command.Color,
            command.Access.UserId,
            now);

        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return new TransitLineManagementResult(
            TransitLineManagementStatus.Success,
            ToCatalogItem(transitLine));
    }

    public async Task<TransitLineManagementResult> PublishAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty) return Invalid("Transit line id cannot be empty.");

        var line = await transitLineRepository.FindByIdAsync(transitLineId, cancellationToken);
        if (line is null)
        {
            return new TransitLineManagementResult(TransitLineManagementStatus.NotFound, Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(access, line))
        {
            return new TransitLineManagementResult(TransitLineManagementStatus.Forbidden, Error: "You do not have permission to publish this transit line.");
        }

        var now = timeProvider.GetUtcNow();
        line.Publish(access.UserId, now);
        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return new TransitLineManagementResult(TransitLineManagementStatus.Success, ToCatalogItem(line));
    }

    public async Task<TransitLineManagementResult> UnpublishAsync(
        TransitLineAccessContext access,
        Guid transitLineId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (transitLineId == Guid.Empty) return Invalid("Transit line id cannot be empty.");

        var line = await transitLineRepository.FindByIdAsync(transitLineId, cancellationToken);
        if (line is null)
        {
            return new TransitLineManagementResult(TransitLineManagementStatus.NotFound, Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(access, line))
        {
            return new TransitLineManagementResult(TransitLineManagementStatus.Forbidden, Error: "You do not have permission to unpublish this transit line.");
        }

        var now = timeProvider.GetUtcNow();
        line.Unpublish(access.UserId, now);
        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return new TransitLineManagementResult(TransitLineManagementStatus.Success, ToCatalogItem(line));
    }

    public async Task<TransitLineStopsResult> AddStopAsync(
        AddStopToLineCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Access.UserId == Guid.Empty)
        {
            return StopsInvalid("Actor user id cannot be empty.");
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return StopsInvalid("Transit line id cannot be empty.");
        }

        if (command.StopId == Guid.Empty)
        {
            return StopsInvalid("Stop id cannot be empty.");
        }

        if (command.ExpectedVersion <= 0)
        {
            return StopsInvalid("Expected version must be greater than zero.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            command.TransitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to manage this transit line.");
        }

        if (transitLine.Status == TransitLineStatus.Archived)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.AlreadyArchived,
                Error: "Archived transit lines cannot be modified.");
        }

        if (transitLine.Version != command.ExpectedVersion)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Conflict,
                Error: "Transit line was modified by another operation.");
        }

        var stopEntity = await stopRepository.FindByIdAsync(
            command.StopId,
            cancellationToken);

        if (stopEntity is null || stopEntity.Status == StopStatus.Archived)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.StopNotFound,
                Error: "The specified stop was not found or is archived.");
        }

        if (transitLine.Stops.Any(s => s.StopId == command.StopId))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.StopAlreadyInLine,
                Error: "The stop is already added to this transit line.");
        }

        var now = timeProvider.GetUtcNow();
        transitLine.AddStop(
            Guid.NewGuid(),
            command.StopId,
            command.Access.UserId,
            now);

        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return await GetStopsAsync(command.Access, command.TransitLineId, cancellationToken);
    }

    public async Task<TransitLineStopsResult> RemoveStopAsync(
        RemoveStopFromLineCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Access.UserId == Guid.Empty)
        {
            return StopsInvalid("Actor user id cannot be empty.");
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return StopsInvalid("Transit line id cannot be empty.");
        }

        if (command.StopId == Guid.Empty)
        {
            return StopsInvalid("Stop id cannot be empty.");
        }

        if (command.ExpectedVersion <= 0)
        {
            return StopsInvalid("Expected version must be greater than zero.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            command.TransitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to manage this transit line.");
        }

        if (transitLine.Status == TransitLineStatus.Archived)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.AlreadyArchived,
                Error: "Archived transit lines cannot be modified.");
        }

        if (transitLine.Version != command.ExpectedVersion)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Conflict,
                Error: "Transit line was modified by another operation.");
        }

        if (!transitLine.Stops.Any(s => s.StopId == command.StopId))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.StopNotInLine,
                Error: "The stop is not part of this transit line.");
        }

        var now = timeProvider.GetUtcNow();
        transitLine.RemoveStop(
            command.StopId,
            command.Access.UserId,
            now);

        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return await GetStopsAsync(command.Access, command.TransitLineId, cancellationToken);
    }

    public async Task<TransitLineStopsResult> ReorderStopsAsync(
        ReorderLineStopsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Access.UserId == Guid.Empty)
        {
            return StopsInvalid("Actor user id cannot be empty.");
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return StopsInvalid("Transit line id cannot be empty.");
        }

        if (command.OrderedStopIds is null)
        {
            return StopsInvalid("Ordered stop ids list cannot be null.");
        }

        if (command.ExpectedVersion <= 0)
        {
            return StopsInvalid("Expected version must be greater than zero.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            command.TransitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to manage this transit line.");
        }

        if (transitLine.Status == TransitLineStatus.Archived)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.AlreadyArchived,
                Error: "Archived transit lines cannot be modified.");
        }

        if (transitLine.Version != command.ExpectedVersion)
        {
            return new TransitLineStopsResult(
                TransitLineManagementStatus.Conflict,
                Error: "Transit line was modified by another operation.");
        }

        var now = timeProvider.GetUtcNow();
        try
        {
            transitLine.ReorderStops(
                command.OrderedStopIds,
                command.Access.UserId,
                now);
        }
        catch (ArgumentException ex)
        {
            return StopsInvalid(ex.Message);
        }

        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return await GetStopsAsync(command.Access, command.TransitLineId, cancellationToken);
    }

    public async Task<TransitLineManagementResult> ArchiveAsync(
        ArchiveTransitLineCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Access.UserId == Guid.Empty)
        {
            return Invalid("Actor user id cannot be empty.");
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return Invalid("Transit line id cannot be empty.");
        }

        if (command.ExpectedVersion <= 0)
        {
            return Invalid("Expected version must be greater than zero.");
        }

        var transitLine = await transitLineRepository.FindByIdAsync(
            command.TransitLineId,
            cancellationToken);

        if (transitLine is null)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.NotFound,
                Error: "Transit line was not found.");
        }

        if (!accessPolicy.CanManage(command.Access, transitLine))
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Forbidden,
                Error: "You do not have permission to manage this transit line.");
        }

        if (transitLine.Status == TransitLineStatus.Archived)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.AlreadyArchived,
                Error: "Transit line is already archived.");
        }

        if (transitLine.Version != command.ExpectedVersion)
        {
            return new TransitLineManagementResult(
                TransitLineManagementStatus.Conflict,
                Error: "Transit line was modified by another operation.");
        }

        var now = timeProvider.GetUtcNow();
        transitLine.Archive(command.Access.UserId, now);

        await transitLineRepository.SaveChangesAsync(cancellationToken);

        return new TransitLineManagementResult(
            TransitLineManagementStatus.Success,
            ToCatalogItem(transitLine));
    }

    private static string? ValidateList(TransitLineListQuery query)
    {
        if (query.Access.UserId == Guid.Empty)
        {
            return "Actor user id cannot be empty.";
        }

        if (query.Page <= 0)
        {
            return "Page must be greater than zero.";
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            return "Page size must be between 1 and 100.";
        }

        return null;
    }

    private static string? ValidateCreate(CreateTransitLineCommand command)
    {
        if (command.Access.UserId == Guid.Empty)
        {
            return "Actor user id cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Transit line name is required.";
        }

        if (command.Name.Trim().Length > 100)
        {
            return "Transit line name cannot exceed 100 characters.";
        }

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return "Transit line code is required.";
        }

        if (command.Code.Trim().Length > 50)
        {
            return "Transit line code cannot exceed 50 characters.";
        }

        if (command.Description?.Trim().Length > 500)
        {
            return "Description cannot exceed 500 characters.";
        }

        return ValidateColor(command.Color);
    }

    private static string? ValidateUpdate(UpdateTransitLineCommand command)
    {
        if (command.Access.UserId == Guid.Empty)
        {
            return "Actor user id cannot be empty.";
        }

        if (command.TransitLineId == Guid.Empty)
        {
            return "Transit line id cannot be empty.";
        }

        if (command.ExpectedVersion <= 0)
        {
            return "Expected version must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Transit line name is required.";
        }

        if (command.Name.Trim().Length > 100)
        {
            return "Transit line name cannot exceed 100 characters.";
        }

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return "Transit line code is required.";
        }

        if (command.Code.Trim().Length > 50)
        {
            return "Transit line code cannot exceed 50 characters.";
        }

        if (command.Description?.Trim().Length > 500)
        {
            return "Description cannot exceed 500 characters.";
        }

        return ValidateColor(command.Color);
    }

    private static string? ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "Color is required.";
        }

        var trimmed = color.Trim();
        if (trimmed.Length != 7
            || trimmed[0] != '#'
            || trimmed.AsSpan(1).ContainsAnyExcept(HexadecimalCharacters))
        {
            return "Color must use #RRGGBB format.";
        }

        return null;
    }

    private static TransitLineManagementResult Invalid(string error) =>
        new(TransitLineManagementStatus.InvalidInput, Error: error);

    private static TransitLineStopsResult StopsInvalid(string error) =>
        new(TransitLineManagementStatus.InvalidInput, Error: error);

    private static TransitLineCatalogItem ToCatalogItem(TransitLine transitLine) =>
        new(
            transitLine.Id,
            transitLine.Name,
            transitLine.Code,
            transitLine.Description,
            transitLine.Color,
            transitLine.Status.ToString(),
            transitLine.OwnerUserId,
            transitLine.CreatedByUserId,
            transitLine.UpdatedByUserId,
            transitLine.CreatedAtUtc,
            transitLine.UpdatedAtUtc,
            transitLine.Version,
            transitLine.Stops.Count);
}
