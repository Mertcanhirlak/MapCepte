using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Transport.Api.Authorization;
using Transport.Api.Identity;
using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.IntegrationTests;

public sealed class AuthEndpointsTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var admin = new User(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "admin@example.com",
            "Initial Admin",
            "TEST-HASH",
            DateTimeOffset.UtcNow);

        var operatorUser = new User(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "operator@example.com",
            "Example Operator",
            "TEST-HASH",
            DateTimeOffset.UtcNow);

        var adminAuthenticationData = new UserAuthenticationData(
            admin,
            [SystemRoleNames.Admin],
            [PermissionNames.UsersManage, PermissionNames.RolesRead]);

        var operatorAuthenticationData = new UserAuthenticationData(
            operatorUser,
            [SystemRoleNames.Operator],
            [PermissionNames.StopsRead]);

        var roles = new RoleCatalogItem[]
        {
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                SystemRoleNames.Admin,
                "System administrator",
                IsSystem: true,
                [PermissionNames.RolesRead, PermissionNames.UsersManage]),
            new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                SystemRoleNames.Operator,
                "Transport operator",
                IsSystem: true,
                [PermissionNames.StopsRead]),
        };

        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddLogging(logging => logging.ClearProviders());
                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();
                    services.RemoveAll<IIdentityRepository>();
                    services.RemoveAll<IPasswordHashService>();
                    services.AddSingleton<IIdentityRepository>(
                        new FakeIdentityRepository(
                            [
                                adminAuthenticationData,
                                operatorAuthenticationData,
                            ],
                            roles));
                    services.AddSingleton<IPasswordHashService>(
                        new AcceptingPasswordHashService());
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task LoginMeAndLogoutUseProtectedCookieSession()
    {
        var anonymousMe = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousMe.StatusCode);

        var anonymousRoles = await _client.GetAsync("/api/admin/roles");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousRoles.StatusCode);

        var csrfResponse = await _client.GetAsync("/api/auth/csrf");
        var csrfBody = await csrfResponse.Content.ReadAsStringAsync();
        Assert.True(
            csrfResponse.IsSuccessStatusCode,
            $"CSRF endpoint failed with {(int)csrfResponse.StatusCode}: {csrfBody}");
        var csrf = await csrfResponse.Content
            .ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(csrf);
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);

        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                "admin@example.com",
                "Strong-Password-2026!"));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(
            login.Headers.GetValues("Set-Cookie"),
            value => value.Contains(
                "MapCepte.Auth=",
                StringComparison.Ordinal));

        var me = await _client.GetFromJsonAsync<AuthenticatedUserResponse>(
            "/api/auth/me");

        Assert.NotNull(me);
        Assert.Equal("admin@example.com", me.Email);
        Assert.Contains(SystemRoleNames.Admin, me.Roles);
        Assert.Contains(PermissionNames.UsersManage, me.Permissions);

        var roles = await _client.GetFromJsonAsync<RoleCatalogResponse[]>(
            "/api/admin/roles");
        Assert.NotNull(roles);
        Assert.Contains(
            roles,
            role => role.Name == SystemRoleNames.Admin);

        var authenticatedCsrf =
            await _client.GetFromJsonAsync<CsrfTokenResponse>(
                "/api/auth/csrf");
        Assert.NotNull(authenticatedCsrf);
        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        _client.DefaultRequestHeaders.Add(
            "X-CSRF-TOKEN",
            authenticatedCsrf.Token);

        var logout = await _client.PostAsync(
            "/api/auth/logout",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var signedOutMe = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, signedOutMe.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutPermissionReceivesForbidden()
    {
        await LoginAsync("operator@example.com");

        var response = await _client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithoutCsrfTokenIsRejected()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                "admin@example.com",
                "Strong-Password-2026!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task LoginAsync(string email)
    {
        var csrf = await _client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/auth/csrf");
        Assert.NotNull(csrf);
        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "Strong-Password-2026!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class AcceptingPasswordHashService : IPasswordHashService
    {
        public string HashPassword(User user, string password)
        {
            return "UPDATED-TEST-HASH";
        }

        public PasswordVerificationOutcome VerifyPassword(
            User user,
            string passwordHash,
            string providedPassword)
        {
            return PasswordVerificationOutcome.Success;
        }

        public void PerformDummyVerification(string providedPassword)
        {
        }
    }

    private sealed class FakeIdentityRepository(
        IReadOnlyCollection<UserAuthenticationData> users,
        IReadOnlyCollection<RoleCatalogItem> roles)
        : IIdentityRepository
    {
        public Task<bool> HasAdminAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<User?> FindUserByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            var user = users
                .SingleOrDefault(candidate =>
                    candidate.User.NormalizedEmail == normalizedEmail)
                ?.User;
            return Task.FromResult(user);
        }

        public Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                users.SingleOrDefault(candidate =>
                    candidate.User.NormalizedEmail == normalizedEmail));
        }

        public Task<Role?> FindRoleByNormalizedNameAsync(
            string normalizedName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Role?>(null);
        }

        public Task<IReadOnlyCollection<RoleCatalogItem>> ListRolesAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(roles);
        }

        public Task AddUserAsync(
            User user,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
