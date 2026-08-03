using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Transport.Api.Authorization;
using Transport.Application.TransitLines;
using Transport.Domain.Identity;

namespace Transport.Api.TransitLines;

public static class TransitLineEndpoints
{
    public static IEndpointRouteBuilder MapTransitLineEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/transit-lines")
            .WithTags("TransitLines");

        group.MapGet(
                "/",
                async (
                    [AsParameters] TransitLineListRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.ListAsync(
                        new TransitLineListQuery(
                            access,
                            request.Search,
                            request.Page,
                            request.PageSize),
                        cancellationToken);

                    if (result.Status != TransitLineManagementStatus.Success
                        || result.Page is null)
                    {
                        return Results.BadRequest(new { error = result.Error });
                    }

                    return Results.Ok(
                        new TransitLinePageResponse(
                            result.Page.Items.Select(ToResponse).ToArray(),
                            result.Page.Page,
                            result.Page.PageSize,
                            result.Page.TotalCount,
                            result.Page.TotalPages));
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        group.MapGet(
                "/{transitLineId:guid}",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.GetByIdAsync(
                        access,
                        transitLineId,
                        cancellationToken);

                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        group.MapGet(
                "/{transitLineId:guid}/stops",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.GetStopsAsync(
                        access,
                        transitLineId,
                        cancellationToken);

                    return ToStopsHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        group.MapPost(
                "/",
                async (
                    CreateTransitLineRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.CreateAsync(
                        new CreateTransitLineCommand(
                            access,
                            request.Name,
                            request.Code,
                            request.Description,
                            request.Color),
                        cancellationToken);

                    return ToHttpResult(result, created: true);
                })
            .RequirePermission(PermissionNames.TransitLinesCreate);

        group.MapPost(
                "/{transitLineId:guid}/stops",
                async (
                    Guid transitLineId,
                    AddTransitLineStopRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.AddStopAsync(
                        new AddStopToLineCommand(
                            access,
                            transitLineId,
                            request.StopId,
                            request.ExpectedVersion),
                        cancellationToken);

                    return ToStopsHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        group.MapPost(
                "/{transitLineId:guid}/publish",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.PublishAsync(access, transitLineId, cancellationToken);
                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        group.MapPost(
                "/{transitLineId:guid}/unpublish",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.UnpublishAsync(access, transitLineId, cancellationToken);
                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        group.MapDelete(
                "/{transitLineId:guid}/stops/{stopId:guid}",
                async (
                    Guid transitLineId,
                    Guid stopId,
                    long version,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.RemoveStopAsync(
                        new RemoveStopFromLineCommand(
                            access,
                            transitLineId,
                            stopId,
                            version),
                        cancellationToken);

                    return ToStopsHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        group.MapPut(
                "/{transitLineId:guid}/stops/order",
                async (
                    Guid transitLineId,
                    ReorderLineStopsRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.ReorderStopsAsync(
                        new ReorderLineStopsCommand(
                            access,
                            transitLineId,
                            request.OrderedStopIds,
                            request.ExpectedVersion),
                        cancellationToken);

                    return ToStopsHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesReorderStops);

        group.MapPut(
                "/{transitLineId:guid}",
                async (
                    Guid transitLineId,
                    UpdateTransitLineRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.UpdateAsync(
                        new UpdateTransitLineCommand(
                            access,
                            transitLineId,
                            request.Name,
                            request.Code,
                            request.Description,
                            request.Color,
                            request.Version),
                        cancellationToken);

                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        group.MapPost(
                "/{transitLineId:guid}/archive",
                async (
                    Guid transitLineId,
                    ArchiveTransitLineRequest request,
                    ClaimsPrincipal principal,
                    TransitLineManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.ArchiveAsync(
                        new ArchiveTransitLineCommand(
                            access,
                            transitLineId,
                            request.Version),
                        cancellationToken);

                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.TransitLinesDelete);

        return endpoints;
    }

    private static IResult ToHttpResult(
        TransitLineManagementResult result,
        bool created)
    {
        if (result.Status == TransitLineManagementStatus.Success
            && result.TransitLine is not null)
        {
            var response = ToResponse(result.TransitLine);
            return created
                ? Results.Created($"/api/transit-lines/{result.TransitLine.Id}", response)
                : Results.Ok(response);
        }

        return result.Status switch
        {
            TransitLineManagementStatus.DuplicateCode => Results.Conflict(
                new { error = result.Error }),
            TransitLineManagementStatus.InvalidInput => Results.BadRequest(
                new { error = result.Error }),
            TransitLineManagementStatus.NotFound => Results.NotFound(
                new { error = result.Error }),
            TransitLineManagementStatus.Forbidden => Results.Json(
                new { error = result.Error },
                statusCode: StatusCodes.Status403Forbidden),
            TransitLineManagementStatus.Conflict
                or TransitLineManagementStatus.AlreadyArchived => Results.Conflict(
                    new { error = result.Error }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult ToStopsHttpResult(TransitLineStopsResult result)
    {
        if (result.Status == TransitLineManagementStatus.Success
            && result.Stops is not null)
        {
            return Results.Ok(result.Stops.Select(ToStopResponse).ToArray());
        }

        return result.Status switch
        {
            TransitLineManagementStatus.StopNotFound => Results.NotFound(
                new { error = result.Error }),
            TransitLineManagementStatus.StopAlreadyInLine => Results.Conflict(
                new { error = result.Error }),
            TransitLineManagementStatus.StopNotInLine => Results.BadRequest(
                new { error = result.Error }),
            TransitLineManagementStatus.InvalidInput => Results.BadRequest(
                new { error = result.Error }),
            TransitLineManagementStatus.NotFound => Results.NotFound(
                new { error = result.Error }),
            TransitLineManagementStatus.Forbidden => Results.Json(
                new { error = result.Error },
                statusCode: StatusCodes.Status403Forbidden),
            TransitLineManagementStatus.Conflict
                or TransitLineManagementStatus.AlreadyArchived => Results.Conflict(
                    new { error = result.Error }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static TransitLineResponse ToResponse(TransitLineCatalogItem item) =>
        new(
            item.Id,
            item.Name,
            item.Code,
            item.Description,
            item.Color,
            item.Status,
            item.OwnerUserId,
            item.CreatedByUserId,
            item.UpdatedByUserId,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.Version,
            item.StopCount);

    private static TransitLineStopResponse ToStopResponse(TransitLineStopItem item) =>
        new(
            item.LineStopId,
            item.StopId,
            item.StopName,
            item.StopCode,
            item.StopColor,
            item.Longitude,
            item.Latitude,
            item.Sequence);

    private static bool TryGetAccessContext(
        ClaimsPrincipal principal,
        out TransitLineAccessContext access)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            access = null!;
            return false;
        }

        access = new TransitLineAccessContext(
            userId,
            principal.IsInRole(SystemRoleNames.Admin),
            principal.IsInRole(SystemRoleNames.Operator));
        return true;
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        return Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}

public sealed record TransitLineListRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CreateTransitLineRequest(
    string Name,
    string Code,
    string? Description,
    string Color);

public sealed record UpdateTransitLineRequest(
    string Name,
    string Code,
    string? Description,
    string Color,
    long Version);

public sealed record ArchiveTransitLineRequest(long Version);

public sealed record AddTransitLineStopRequest(
    Guid StopId,
    long ExpectedVersion);

public sealed record ReorderLineStopsRequest(
    IReadOnlyList<Guid> OrderedStopIds,
    long ExpectedVersion);

public sealed record TransitLineStopResponse(
    Guid LineStopId,
    Guid StopId,
    string StopName,
    string? StopCode,
    string StopColor,
    double Longitude,
    double Latitude,
    int Sequence);

public sealed record TransitLineResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Color,
    string Status,
    Guid OwnerUserId,
    Guid CreatedByUserId,
    Guid UpdatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version,
    int StopCount);

public sealed record TransitLinePageResponse(
    IReadOnlyCollection<TransitLineResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
