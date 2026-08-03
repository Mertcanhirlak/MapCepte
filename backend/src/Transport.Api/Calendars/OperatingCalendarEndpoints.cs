using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Transport.Api.Authorization;
using Transport.Application.Calendars;
using Transport.Domain.Calendars;
using Transport.Domain.Identity;

namespace Transport.Api.Calendars;

public static class OperatingCalendarEndpoints
{
    public static IEndpointRouteBuilder MapOperatingCalendarEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operating-calendars")
            .WithTags("OperatingCalendars");

        group.MapGet(
                "/",
                async (
                    OperatingCalendarManagementService service,
                    CancellationToken cancellationToken) =>
                {
                    var calendars = await service.ListAllAsync(cancellationToken);
                    return Results.Ok(calendars);
                })
            .RequirePermission(PermissionNames.TransitLinesRead);

        group.MapPost(
                "/",
                async (
                    CreateCalendarRequest request,
                    ClaimsPrincipal principal,
                    OperatingCalendarManagementService service,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    if (!TryGetUserId(principal, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    if (!Enum.TryParse<DaysOfWeek>(request.DaysOfWeek, true, out var days))
                    {
                        return Results.BadRequest(new { error = "Invalid days of week value." });
                    }

                    var calendar = await service.CreateCalendarAsync(
                        request.Name,
                        days,
                        userId,
                        cancellationToken);

                    return Results.Ok(calendar);
                })
            .RequirePermission(PermissionNames.TransitLinesCreate);

        return endpoints;
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

public sealed record CreateCalendarRequest(
    string Name,
    string DaysOfWeek);
