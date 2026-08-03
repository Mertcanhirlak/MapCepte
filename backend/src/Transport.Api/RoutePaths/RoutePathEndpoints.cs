using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Transport.Api.Authorization;
using Transport.Application.RoutePaths;
using Transport.Application.TransitLines;
using Transport.Domain.Identity;
using Transport.Domain.RoutePaths;

namespace Transport.Api.RoutePaths;

public static class RoutePathEndpoints
{
    public static IEndpointRouteBuilder MapRoutePathEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var transitLineGroup = endpoints
            .MapGroup("/api/transit-lines/{transitLineId:guid}/route-paths")
            .WithTags("RoutePaths");

        transitLineGroup.MapGet(
                "/",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    RoutePathManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var routePaths = await service.ListByTransitLineAsync(
                        access,
                        transitLineId,
                        cancellationToken);

                    return Results.Ok(routePaths.Select(ToResponse).ToArray());
                })
            .RequirePermission(PermissionNames.RoutePathsRead);

        transitLineGroup.MapPost(
                "/generate",
                async (
                    Guid transitLineId,
                    GenerateRoutePathRequest request,
                    ClaimsPrincipal principal,
                    RoutePathManagementService service,
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

                    if (!Enum.TryParse<RoutePathDirection>(request.Direction, true, out var direction))
                    {
                        return Results.BadRequest(new { error = "Invalid direction value." });
                    }

                    var result = await service.GenerateAsync(
                        new GenerateRoutePathCommand(
                            access,
                            transitLineId,
                            request.Name,
                            direction,
                            request.ColorOverride),
                        cancellationToken);

                    return ToHttpResult(result);
                })
            .RequirePermission(PermissionNames.RoutePathsGenerate);

        var routePathGroup = endpoints
            .MapGroup("/api/route-paths")
            .WithTags("RoutePaths");

        routePathGroup.MapGet(
                "/{routePathId:guid}",
                async (
                    Guid routePathId,
                    ClaimsPrincipal principal,
                    RoutePathManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.GetByIdAsync(
                        access,
                        routePathId,
                        cancellationToken);

                    return ToHttpResult(result);
                })
            .RequirePermission(PermissionNames.RoutePathsRead);

        return endpoints;
    }

    private static IResult ToHttpResult(RoutePathResult result)
    {
        if (result.Status == RoutePathManagementStatus.Success && result.RoutePath is not null)
        {
            return Results.Ok(ToResponse(result.RoutePath));
        }

        return result.Status switch
        {
            RoutePathManagementStatus.InvalidInput
                or RoutePathManagementStatus.InsufficientStops => Results.BadRequest(new { error = result.Error }),
            RoutePathManagementStatus.NotFound => Results.NotFound(new { error = result.Error }),
            RoutePathManagementStatus.Forbidden => Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status403Forbidden),
            RoutePathManagementStatus.GenerationFailed => Results.UnprocessableEntity(new { error = result.Error, routePath = result.RoutePath is not null ? ToResponse(result.RoutePath) : null }),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static RoutePathResponse ToResponse(RoutePathCatalogItem item) =>
        new(
            item.Id,
            item.TransitLineId,
            item.Name,
            item.Direction,
            item.Version,
            item.Status,
            item.ColorOverride,
            item.DistanceMeters,
            item.DurationSeconds,
            item.RoutingEngine,
            item.GeneratedAtUtc,
            item.FailureCode,
            item.FailureMessage,
            item.StopCount,
            item.Coordinates);

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

public sealed record GenerateRoutePathRequest(
    string Name,
    string Direction,
    string? ColorOverride = null);

public sealed record RoutePathResponse(
    Guid Id,
    Guid TransitLineId,
    string Name,
    string Direction,
    int Version,
    string Status,
    string? ColorOverride,
    double DistanceMeters,
    double DurationSeconds,
    string RoutingEngine,
    DateTimeOffset? GeneratedAtUtc,
    string? FailureCode,
    string? FailureMessage,
    int StopCount,
    double[][]? Coordinates);
