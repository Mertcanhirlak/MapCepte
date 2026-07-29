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

        return endpoints;
    }
}
