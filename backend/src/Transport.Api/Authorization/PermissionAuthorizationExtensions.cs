using Microsoft.AspNetCore.Authorization;
using Transport.Domain.Identity;

namespace Transport.Api.Authorization;

public static class PermissionAuthorizationExtensions
{
    private const string PolicyPrefix = "permission:";

    public static IServiceCollection AddPermissionAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionNames.All)
            {
                options.AddPolicy(
                    GetPolicyName(permission),
                    policy => policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(
                            new PermissionRequirement(permission)));
            }
        });

        services.AddSingleton<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        return services;
    }

    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder builder,
        string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(GetPolicyName(permission));
        return builder;
    }

    private static string GetPolicyName(string permission)
    {
        if (!PermissionNames.All.Contains(permission, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(permission),
                permission,
                "Permission is not part of the system catalog.");
        }

        return $"{PolicyPrefix}{permission}";
    }
}
