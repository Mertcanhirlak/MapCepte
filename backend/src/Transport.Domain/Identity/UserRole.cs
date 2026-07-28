namespace Transport.Domain.Identity;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId, DateTimeOffset assignedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc.ToUniversalTime();
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    public Role Role { get; private set; } = null!;
}
