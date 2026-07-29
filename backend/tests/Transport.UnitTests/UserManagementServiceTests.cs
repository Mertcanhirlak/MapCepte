using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.UnitTests;

public sealed class UserManagementServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesUserWithHashedPasswordAndSelectedRoles()
    {
        var repository = new FakeIdentityRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateUserCommand(
                "operator@example.com",
                "Example Operator",
                "Strong-Password-2026!",
                [SystemRoleNames.Operator, SystemRoleNames.User]));

        Assert.Equal(UserManagementStatus.Success, result.Status);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal(
            "HASH::Strong-Password-2026!",
            repository.AddedUser.PasswordHash);
        Assert.Contains(
            repository.OperatorRole.Id,
            repository.AddedUser.UserRoles.Select(role => role.RoleId));
        Assert.Contains(
            repository.UserRole.Id,
            repository.AddedUser.UserRoles.Select(role => role.RoleId));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task RejectsDuplicateEmail()
    {
        var repository = new FakeIdentityRepository();
        repository.ExistingUser = CreateUser(
            "operator@example.com",
            repository.OperatorRole.Id);
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateUserCommand(
                "operator@example.com",
                "Duplicate Operator",
                "Strong-Password-2026!",
                [SystemRoleNames.Operator]));

        Assert.Equal(UserManagementStatus.DuplicateEmail, result.Status);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task RejectsWeakPasswordWithoutDevelopmentOverride()
    {
        var repository = new FakeIdentityRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateUserCommand(
                "user@example.com",
                "Example User",
                "123456",
                [SystemRoleNames.User]));

        Assert.Equal(UserManagementStatus.InvalidInput, result.Status);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task AcceptsSixCharacterPasswordWithDevelopmentOverride()
    {
        var repository = new FakeIdentityRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateUserCommand(
                "user@example.com",
                "Example User",
                "123456",
                [SystemRoleNames.User],
                AllowWeakPassword: true));

        Assert.Equal(UserManagementStatus.Success, result.Status);
        Assert.Equal("HASH::123456", repository.AddedUser?.PasswordHash);
    }

    [Fact]
    public async Task ReplacesRolesForAnotherUser()
    {
        var repository = new FakeIdentityRepository();
        var target = CreateUser(
            "operator@example.com",
            repository.OperatorRole.Id);
        repository.ExistingUser = target;
        var service = CreateService(repository);

        var result = await service.UpdateRolesAsync(
            new UpdateUserRolesCommand(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                target.Id,
                [SystemRoleNames.User]));

        Assert.Equal(UserManagementStatus.Success, result.Status);
        Assert.Equal(
            [repository.UserRole.Id],
            target.UserRoles.Select(role => role.RoleId));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ForbidsChangingOwnRoles()
    {
        var repository = new FakeIdentityRepository();
        var target = CreateUser(
            "admin@example.com",
            repository.AdminRole.Id);
        repository.ExistingUser = target;
        var service = CreateService(repository);

        var result = await service.UpdateRolesAsync(
            new UpdateUserRolesCommand(
                target.Id,
                target.Id,
                [SystemRoleNames.User]));

        Assert.Equal(
            UserManagementStatus.SelfRoleChangeForbidden,
            result.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    private static UserManagementService CreateService(
        FakeIdentityRepository repository)
    {
        return new UserManagementService(
            repository,
            new FakePasswordHashService(),
            new FixedTimeProvider(FixedNow));
    }

    private static User CreateUser(string email, Guid roleId)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            "Test User",
            "HASH",
            FixedNow);
        user.AssignRole(roleId, FixedNow);
        return user;
    }

    private sealed class FakeIdentityRepository : IIdentityRepository
    {
        public Role AdminRole { get; } = new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SystemRoleNames.Admin,
            "System administrator",
            isSystem: true);

        public Role OperatorRole { get; } = new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            SystemRoleNames.Operator,
            "Transport operator",
            isSystem: true);

        public Role UserRole { get; } = new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SystemRoleNames.User,
            "Passenger",
            isSystem: true);

        public User? ExistingUser { get; set; }

        public User? AddedUser { get; private set; }

        public int SaveCount { get; private set; }

        public Task<bool> HasAdminAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<User?> FindUserByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExistingUser?.NormalizedEmail == normalizedEmail
                    ? ExistingUser
                    : null);
        }

        public Task<User?> FindUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExistingUser?.Id == userId ? ExistingUser : null);
        }

        public Task<UserAuthenticationData?> FindUserAuthenticationDataAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAuthenticationData?>(null);
        }

        public Task<Role?> FindRoleByNormalizedNameAsync(
            string normalizedName,
            CancellationToken cancellationToken)
        {
            var role = new[] { AdminRole, OperatorRole, UserRole }
                .SingleOrDefault(candidate =>
                    candidate.NormalizedName == normalizedName);
            return Task.FromResult(role);
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
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(User user, string password)
        {
            return $"HASH::{password}";
        }

        public PasswordVerificationOutcome VerifyPassword(
            User user,
            string passwordHash,
            string providedPassword)
        {
            return PasswordVerificationOutcome.Failed;
        }

        public void PerformDummyVerification(string providedPassword)
        {
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
