using Transport.Domain.Identity;

namespace Transport.UnitTests;

public sealed class IdentityDomainTests
{
    [Fact]
    public void UserNormalizesEmailAndStartsActive()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            28,
            12,
            0,
            0,
            TimeSpan.FromHours(3));

        var user = new User(
            Guid.NewGuid(),
            "  operator@example.com  ",
            "  Example Operator  ",
            "test-password-hash",
            createdAt);

        Assert.Equal("operator@example.com", user.Email);
        Assert.Equal("OPERATOR@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("Example Operator", user.DisplayName);
        Assert.True(user.IsActive);
        Assert.Equal(TimeSpan.Zero, user.CreatedAtUtc.Offset);
    }

    [Fact]
    public void UserCanBeDeactivatedAndReactivated()
    {
        var user = new User(
            Guid.NewGuid(),
            "user@example.com",
            "Example User",
            "test-password-hash",
            DateTimeOffset.UtcNow);

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void SystemRolesAndPermissionsAreUnique()
    {
        Assert.Equal(3, SystemRoleNames.All.Count);
        Assert.Equal(
            SystemRoleNames.All.Count,
            SystemRoleNames.All.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal(18, PermissionNames.All.Count);
        Assert.Equal(
            PermissionNames.All.Count,
            PermissionNames.All.Distinct(StringComparer.Ordinal).Count());
    }
}
