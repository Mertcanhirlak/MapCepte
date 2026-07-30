using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;
using Transport.Api.Identity;
using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.Api.Authorization;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin")
            .WithTags("Administration");

        group.MapGet(
                "/roles",
                async (
                    RoleCatalogService roleCatalogService,
                    CancellationToken cancellationToken) =>
                {
                    var roles = await roleCatalogService.ListAsync(
                        cancellationToken);

                    return TypedResults.Ok(
                        roles.Select(role => new RoleCatalogResponse(
                            role.Id,
                            role.Name,
                            role.Description,
                            role.IsSystem,
                            role.Permissions)));
                })
            .RequirePermission(PermissionNames.RolesRead);

        group.MapGet(
                "/users",
                async (
                    UserManagementService userManagementService,
                    CancellationToken cancellationToken) =>
                {
                    var users = await userManagementService.ListAsync(
                        cancellationToken);

                    return TypedResults.Ok(users.Select(ToResponse));
                })
            .RequirePermission(PermissionNames.UsersRead);

        group.MapGet(
                "/audit",
                async (
                    AuditCatalogService auditCatalogService,
                    CancellationToken cancellationToken) =>
                {
                    var entries =
                        await auditCatalogService.ListRecentAsync(
                            cancellationToken);

                    return TypedResults.Ok(
                        entries.Select(entry => new AuditCatalogResponse(
                            entry.Id,
                            entry.EventType,
                            entry.Outcome,
                            entry.OccurredAtUtc,
                            entry.ActorUserId,
                            entry.SubjectUserId,
                            entry.IpAddress)));
                })
            .RequirePermission(PermissionNames.AuditRead);

        group.MapPost(
                "/users",
                async (
                    CreateUserRequest request,
                    ClaimsPrincipal principal,
                    UserManagementService userManagementService,
                    IOptions<IdentitySecurityOptions> securityOptions,
                    IHostEnvironment environment,
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

                    var result = await userManagementService.CreateAsync(
                        new CreateUserCommand(
                            request.Email,
                            request.DisplayName,
                            request.Password,
                            request.Roles,
                            AllowWeakPassword:
                                environment.IsDevelopment()
                                && securityOptions.Value
                                    .AllowWeakPasswordsInDevelopment,
                            ActorUserId: actorUserId),
                        cancellationToken);

                    return ToHttpResult(result, created: true);
                })
            .RequirePermission(PermissionNames.UsersManage)
            .RequirePermission(PermissionNames.RolesManage);

        group.MapPut(
                "/users/{userId:guid}/roles",
                async (
                    Guid userId,
                    UpdateUserRolesRequest request,
                    ClaimsPrincipal principal,
                    UserManagementService userManagementService,
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

                    var result =
                        await userManagementService.UpdateRolesAsync(
                            new UpdateUserRolesCommand(
                                actorUserId,
                                userId,
                                request.Roles),
                            cancellationToken);

                    return ToHttpResult(result, created: false);
                })
            .RequirePermission(PermissionNames.UsersManage)
            .RequirePermission(PermissionNames.RolesManage);

        return endpoints;
    }

    private static IResult ToHttpResult(
        UserManagementResult result,
        bool created)
    {
        if (result.Status == UserManagementStatus.Success
            && result.User is not null)
        {
            var response = ToResponse(result.User);
            return created
                ? Results.Created(
                    $"/api/admin/users/{result.User.Id}",
                    response)
                : Results.Ok(response);
        }

        return result.Status switch
        {
            UserManagementStatus.DuplicateEmail => Results.Conflict(
                new { error = result.Error }),
            UserManagementStatus.UserNotFound => Results.NotFound(
                new { error = result.Error }),
            UserManagementStatus.SelfRoleChangeForbidden =>
                Results.Json(
                    new { error = result.Error },
                    statusCode: StatusCodes.Status403Forbidden),
            UserManagementStatus.InvalidInput
                or UserManagementStatus.UnknownRole => Results.BadRequest(
                    new { error = result.Error }),
            _ => Results.Problem(
                statusCode:
                    StatusCodes.Status500InternalServerError),
        };
    }

    private static UserCatalogResponse ToResponse(UserCatalogItem user)
    {
        return new UserCatalogResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.CreatedAtUtc,
            user.Roles);
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
