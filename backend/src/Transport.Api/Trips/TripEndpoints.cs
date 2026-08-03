using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Transport.Api.Authorization;
using Transport.Application.TransitLines;
using Transport.Application.Trips;
using Transport.Domain.Identity;
using Transport.Domain.RoutePaths;

namespace Transport.Api.Trips;

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var transitLineGroup = endpoints
            .MapGroup("/api/transit-lines/{transitLineId:guid}")
            .WithTags("Trips");

        transitLineGroup.MapGet(
                "/trips",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TripManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var trips = await service.ListByTransitLineAsync(
                        access,
                        transitLineId,
                        cancellationToken);

                    return Results.Ok(trips);
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        transitLineGroup.MapGet(
                "/timetable",
                async (
                    Guid transitLineId,
                    ClaimsPrincipal principal,
                    TripManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetAccessContext(principal, out var access))
                    {
                        return Results.Unauthorized();
                    }

                    var timetable = await service.GetTimetableMatrixAsync(
                        access,
                        transitLineId,
                        cancellationToken);

                    return timetable is null
                        ? Results.NotFound()
                        : Results.Ok(timetable);
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        transitLineGroup.MapPost(
                "/trips",
                async (
                    Guid transitLineId,
                    CreateTripRequest request,
                    ClaimsPrincipal principal,
                    TripManagementService service,
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

                    var result = await service.CreateTripAsync(
                        new CreateTripCommand(
                            access,
                            transitLineId,
                            request.RoutePathId,
                            request.OperatingCalendarId,
                            request.TripCode,
                            request.DepartureTime,
                            direction),
                        cancellationToken);

                    return ToHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        var tripGroup = endpoints
            .MapGroup("/api/trips")
            .WithTags("Trips");

        tripGroup.MapPost(
                "/{tripId:guid}/shift",
                async (
                    Guid tripId,
                    ShiftTripTimeRequest request,
                    ClaimsPrincipal principal,
                    TripManagementService service,
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

                    var result = await service.ShiftTripTimeAsync(
                        new ShiftTripTimeCommand(
                            access,
                            tripId,
                            request.MinutesOffset),
                        cancellationToken);

                    return ToHttpResult(result);
                })
            .RequirePermission(PermissionNames.TransitLinesUpdate);

        return endpoints;
    }

    private static IResult ToHttpResult(TripResult result)
    {
        if (result.Status == TripManagementStatus.Success && result.Trip is not null)
        {
            return Results.Ok(result.Trip);
        }

        return result.Status switch
        {
            TripManagementStatus.InvalidInput => Results.BadRequest(new { error = result.Error }),
            TripManagementStatus.NotFound => Results.NotFound(new { error = result.Error }),
            TripManagementStatus.Forbidden => Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status403Forbidden),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

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

public sealed record CreateTripRequest(
    Guid RoutePathId,
    Guid OperatingCalendarId,
    string TripCode,
    TimeOnly DepartureTime,
    string Direction);

public sealed record ShiftTripTimeRequest(
    int MinutesOffset);
