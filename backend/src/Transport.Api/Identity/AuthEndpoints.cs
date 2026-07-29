using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Transport.Application.Identity;

namespace Transport.Api.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapGet(
                "/csrf",
                (HttpContext httpContext, IAntiforgery antiforgery) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(httpContext);
                    return TypedResults.Ok(
                        new CsrfTokenResponse(tokens.RequestToken!));
                })
            .AllowAnonymous();

        group.MapPost(
                "/login",
                async (
                    LoginRequest request,
                    LoginService loginService,
                    IAntiforgery antiforgery,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    var result = await loginService.LoginAsync(
                        new LoginCommand(request.Email, request.Password),
                        cancellationToken);

                    if (result.Status != LoginStatus.Success
                        || result.User is null)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Invalid email or password.");
                    }

                    var principal = CreatePrincipal(result.User);
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            AllowRefresh = true,
                            IsPersistent = false,
                        });

                    return Results.Ok(ToResponse(result.User));
                })
            .AllowAnonymous()
            .RequireRateLimiting("auth-login");

        group.MapPost(
                "/logout",
                async (
                    HttpContext httpContext,
                    IAntiforgery antiforgery) =>
                {
                    if (!await antiforgery.IsRequestValidAsync(httpContext))
                    {
                        return Results.BadRequest();
                    }

                    await httpContext.SignOutAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.NoContent();
                })
            .RequireAuthorization();

        group.MapGet(
                "/me",
                (ClaimsPrincipal principal) =>
                {
                    var response = ToResponse(principal);
                    return response is null
                        ? Results.Unauthorized()
                        : Results.Ok(response);
                })
            .RequireAuthorization();

        return endpoints;
    }

    private static ClaimsPrincipal CreatePrincipal(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
        };

        claims.AddRange(
            user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(
            user.Permissions.Select(
                permission => new Claim(
                    AuthClaimTypes.Permission,
                    permission)));

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role));
    }

    private static AuthenticatedUserResponse ToResponse(
        AuthenticatedUser user)
    {
        return new AuthenticatedUserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Roles,
            user.Permissions);
    }

    private static AuthenticatedUserResponse? ToResponse(
        ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var displayName = principal.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(idValue, out var id)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        return new AuthenticatedUserResponse(
            id,
            email,
            displayName,
            principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            principal.FindAll(AuthClaimTypes.Permission)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray());
    }
}
