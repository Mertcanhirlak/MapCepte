using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.UnitTests;

public sealed class AdminBootstrapServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesOneAdminWithHashedPasswordAndRole()
    {
        var repository = new FakeIdentityRepository();
        var passwordService = new FakePasswordHashService();
        var service = CreateService(repository, passwordService);

        var result = await service.BootstrapAsync(
            new BootstrapAdminCommand(
                "admin@example.com",
                "Initial Admin",
                "Strong-Password-2026!"));

        Assert.Equal(BootstrapAdminStatus.Created, result.Status);
        Assert.NotNull(result.UserId);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal(
            "HASH::Strong-Password-2026!",
            repository.AddedUser.PasswordHash);
        Assert.Single(repository.AddedUser.UserRoles);
        Assert.Equal(
            repository.AdminRole.Id,
            repository.AddedUser.UserRoles.Single().RoleId);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ExistingAdminMakesBootstrapIdempotent()
    {
        var repository = new FakeIdentityRepository
        {
            AdminExists = true,
        };
        var passwordService = new FakePasswordHashService();
        var service = CreateService(repository, passwordService);

        var result = await service.BootstrapAsync(
            new BootstrapAdminCommand(
                "admin@example.com",
                "Initial Admin",
                "Strong-Password-2026!"));

        Assert.Equal(BootstrapAdminStatus.AlreadyConfigured, result.Status);
        Assert.Null(repository.AddedUser);
        Assert.Equal(0, passwordService.HashCallCount);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase2026!")]
    [InlineData("ALLUPPERCASE2026!")]
    [InlineData("NoNumbersHere!")]
    [InlineData("NoSymbolsHere2026")]
    public async Task RejectsWeakBootstrapPassword(string password)
    {
        var service = CreateService(
            new FakeIdentityRepository(),
            new FakePasswordHashService());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.BootstrapAsync(
                new BootstrapAdminCommand(
                    "admin@example.com",
                    "Initial Admin",
                    password)));
    }

    [Fact]
    public async Task AllowsSixCharacterPasswordWhenDevelopmentOverrideIsExplicit()
    {
        var repository = new FakeIdentityRepository();
        var passwordService = new FakePasswordHashService();
        var service = CreateService(repository, passwordService);

        var result = await service.BootstrapAsync(
            new BootstrapAdminCommand(
                "admin@example.com",
                "Initial Admin",
                "123456",
                AllowWeakPassword: true));

        Assert.Equal(BootstrapAdminStatus.Created, result.Status);
        Assert.Equal("HASH::123456", repository.AddedUser?.PasswordHash);
    }

    private static AdminBootstrapService CreateService(
        IIdentityRepository repository,
        IPasswordHashService passwordService)
    {
        return new AdminBootstrapService(
            repository,
            passwordService,
            new FixedTimeProvider(FixedNow));
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public int HashCallCount { get; private set; }

        public string HashPassword(User user, string password)
        {
            HashCallCount++;
            return $"HASH::{password}";
        }

        public PasswordVerificationOutcome VerifyPassword(
            User user,
            string passwordHash,
            string providedPassword)
        {
            return passwordHash == $"HASH::{providedPassword}"
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed;
        }

        public void PerformDummyVerification(string providedPassword)
        {
        }
    }

    private sealed class FakeIdentityRepository : IIdentityRepository
    {
        public Role AdminRole { get; } = new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SystemRoleNames.Admin,
            "System administrator",
            isSystem: true);

        public bool AdminExists { get; init; }

        public User? AddedUser { get; private set; }

        public int SaveCount { get; private set; }

        public Task<bool> HasAdminAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(AdminExists);
        }

        public Task<User?> FindUserByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<Role?> FindRoleByNormalizedNameAsync(
            string normalizedName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Role?>(AdminRole);
        }

        public Task<IReadOnlyCollection<RoleCatalogItem>> ListRolesAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<RoleCatalogItem>>([]);
        }

        public Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAuthenticationData?>(null);
        }

        public Task AddUserAsync(
            User user,
            CancellationToken cancellationToken)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
