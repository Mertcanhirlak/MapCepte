using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Transport.Api.Authorization;
using Transport.Application.Stops;
using Transport.Domain.Identity;

namespace Transport.Api.Stops;

public static class StopEndpoints
{
    public static IEndpointRouteBuilder MapStopEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/stops")
            .WithTags("Stops");

        group.MapGet(
                "/",
                async (
                    ClaimsPrincipal principal,
                    StopManagementService stopManagementService,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var stops = await stopManagementService.ListAsync(
                        access,
                        cancellationToken);

                    return Results.Ok(stops.Select(ToResponse));
                })
            .RequirePermission(PermissionNames.StopsRead);

        group.MapPost(
                "/",
                async (
                    CreateStopRequest request,
                    ClaimsPrincipal principal,
                    StopManagementService stopManagementService,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetUserId(principal, out var actorUserId))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await stopManagementService.CreateAsync(
                        new CreateStopCommand(
                            actorUserId,
                            request.Name,
                            request.Code,
                            request.Description,
                            request.Color,
                            request.Longitude,
                            request.Latitude),
                        cancellationToken);

                    return ToHttpResult(result);
                })
            .RequirePermission(PermissionNames.StopsCreate);

        return endpoints;
    }

    private static IResult ToHttpResult(StopManagementResult result)
    {
        if (result.Status == StopManagementStatus.Success
            && result.Stop is not null)
        {
            return Results.Created(
                $"/api/stops/{result.Stop.Id}",
                ToResponse(result.Stop));
        }

        return result.Status switch
        {
            StopManagementStatus.DuplicateCode => Results.Conflict(
                new { error = result.Error }),
            StopManagementStatus.InvalidInput => Results.BadRequest(
                new { error = result.Error }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static StopResponse ToResponse(StopCatalogItem stop)
    {
        return new StopResponse(
            stop.Id,
            stop.Name,
            stop.Code,
            stop.Description,
            stop.Color,
            stop.Longitude,
            stop.Latitude,
            stop.Status,
            stop.CreatedByUserId,
            stop.CreatedAtUtc);
    }

    private static bool TryGetAccessContext(
        ClaimsPrincipal principal,
        out StopAccessContext access)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            access = null!;
            return false;
        }

        access = new StopAccessContext(
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
