using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.UnitTests;

public sealed class LoginServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidCredentialsReturnRolesAndPermissions()
    {
        var user = CreateUser();
        var repository = new FakeIdentityRepository(
            new UserAuthenticationData(
                user,
                [SystemRoleNames.Admin],
                [PermissionNames.UsersManage]));
        var passwordService = new FakePasswordHashService(
            PasswordVerificationOutcome.Success);
        var service = CreateService(repository, passwordService);

        var result = await service.LoginAsync(
            new LoginCommand(
                " ADMIN@EXAMPLE.COM ",
                "Strong-Password-2026!"));

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Contains(SystemRoleNames.Admin, result.User.Roles);
        Assert.Contains(PermissionNames.UsersManage, result.User.Permissions);
        Assert.Equal("ADMIN@EXAMPLE.COM", repository.RequestedEmail);
    }

    [Fact]
    public async Task UnknownEmailUsesDummyVerificationAndReturnsGenericFailure()
    {
        var repository = new FakeIdentityRepository(authenticationData: null);
        var passwordService = new FakePasswordHashService(
            PasswordVerificationOutcome.Success);
        var service = CreateService(repository, passwordService);

        var result = await service.LoginAsync(
            new LoginCommand(
                "missing@example.com",
                "Strong-Password-2026!"));

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
        Assert.Equal(1, passwordService.DummyVerificationCount);
        Assert.Equal(0, passwordService.PasswordVerificationCount);
    }

    [Fact]
    public async Task InactiveUserCannotLogin()
    {
        var user = CreateUser();
        user.Deactivate();
        var repository = new FakeIdentityRepository(
            new UserAuthenticationData(user, [], []));
        var service = CreateService(
            repository,
            new FakePasswordHashService(
                PasswordVerificationOutcome.Success));

        var result = await service.LoginAsync(
            new LoginCommand(
                "admin@example.com",
                "Strong-Password-2026!"));

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
    }

    [Fact]
    public async Task RehashesPasswordWhenHasherRequestsUpgrade()
    {
        var user = CreateUser();
        var repository = new FakeIdentityRepository(
            new UserAuthenticationData(user, [], []));
        var passwordService = new FakePasswordHashService(
            PasswordVerificationOutcome.SuccessRehashNeeded);
        var service = CreateService(repository, passwordService);

        var result = await service.LoginAsync(
            new LoginCommand(
                "admin@example.com",
                "Strong-Password-2026!"));

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("UPGRADED-HASH", user.PasswordHash);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task LocksUserAfterConfiguredFailedAttempts()
    {
        var user = CreateUser();
        var repository = new FakeIdentityRepository(
            new UserAuthenticationData(user, [], []));
        var auditStore = new FakeAuditStore();
        var service = CreateService(
            repository,
            new FakePasswordHashService(
                PasswordVerificationOutcome.Failed),
            auditStore,
            new LoginSecurityPolicy(3, TimeSpan.FromMinutes(10)));

        var first = await service.LoginAsync(
            new LoginCommand("admin@example.com", "wrong", "127.0.0.1"));
        var second = await service.LoginAsync(
            new LoginCommand("admin@example.com", "wrong", "127.0.0.1"));
        var third = await service.LoginAsync(
            new LoginCommand("admin@example.com", "wrong", "127.0.0.1"));

        Assert.Equal(LoginStatus.InvalidCredentials, first.Status);
        Assert.Equal(LoginStatus.InvalidCredentials, second.Status);
        Assert.Equal(LoginStatus.LockedOut, third.Status);
        Assert.True(user.IsLockedOut(FixedNow));
        Assert.Equal(3, user.FailedLoginAttemptCount);
        Assert.Equal(
            AuditOutcomes.LockedOut,
            auditStore.Entries.Last().Outcome);
        Assert.DoesNotContain(
            auditStore.Entries,
            entry => entry.EventType.Contains(
                "password",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SuccessfulLoginClearsPreviousFailedAttempts()
    {
        var user = CreateUser();
        user.RegisterFailedLogin(
            FixedNow,
            maximumAttempts: 5,
            lockoutDuration: TimeSpan.FromMinutes(15));
        var repository = new FakeIdentityRepository(
            new UserAuthenticationData(user, [], []));
        var auditStore = new FakeAuditStore();
        var service = CreateService(
            repository,
            new FakePasswordHashService(
                PasswordVerificationOutcome.Success),
            auditStore);

        var result = await service.LoginAsync(
            new LoginCommand("admin@example.com", "correct"));

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal(0, user.FailedLoginAttemptCount);
        Assert.Null(user.LockoutEndUtc);
        Assert.Equal(
            AuditOutcomes.Succeeded,
            auditStore.Entries.Single().Outcome);
    }

    private static LoginService CreateService(
        FakeIdentityRepository repository,
        FakePasswordHashService passwordService,
        FakeAuditStore? auditStore = null,
        LoginSecurityPolicy? securityPolicy = null)
    {
        return new LoginService(
            repository,
            passwordService,
            auditStore ?? new FakeAuditStore(),
            securityPolicy ?? new LoginSecurityPolicy(),
            new FixedTimeProvider(FixedNow));
    }

    private static User CreateUser()
    {
        return new User(
            Guid.NewGuid(),
            "admin@example.com",
            "Initial Admin",
            "CURRENT-HASH",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakePasswordHashService(
        PasswordVerificationOutcome outcome)
        : IPasswordHashService
    {
        public int DummyVerificationCount { get; private set; }

        public int PasswordVerificationCount { get; private set; }

        public string HashPassword(User user, string password)
        {
            return "UPGRADED-HASH";
        }

        public PasswordVerificationOutcome VerifyPassword(
            User user,
            string passwordHash,
            string providedPassword)
        {
            PasswordVerificationCount++;
            return outcome;
        }

        public void PerformDummyVerification(string providedPassword)
        {
            DummyVerificationCount++;
        }
    }

    private sealed class FakeIdentityRepository(
        UserAuthenticationData? authenticationData)
        : IIdentityRepository
    {
        public string? RequestedEmail { get; private set; }

        public int SaveCount { get; private set; }

        public Task<bool> HasAdminAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<User?> FindUserByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(authenticationData?.User);
        }

        public Task<User?> FindUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            RequestedEmail = normalizedEmail;
            return Task.FromResult(authenticationData);
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
            return Task.FromResult<IReadOnlyCollection<RoleCatalogItem>>([]);
        }

        public Task<IReadOnlyCollection<UserCatalogItem>> ListUsersAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<UserCatalogItem>>([]);
        }

        public Task AddUserAsync(
            User user,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditStore : IAuditStore
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task AddAsync(
            AuditEntry auditEntry,
            CancellationToken cancellationToken)
        {
            Entries.Add(auditEntry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuditCatalogItem>> ListRecentAsync(
            int maximumCount,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AuditCatalogItem>>([]);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
