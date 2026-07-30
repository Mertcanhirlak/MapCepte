namespace Transport.Domain.Identity;

public sealed class User
{
    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(id));
        }

        Id = id;
        Email = RequireText(email, nameof(email));
        NormalizedEmail = Email.ToUpperInvariant();
        DisplayName = RequireText(displayName, nameof(displayName));
        PasswordHash = RequireText(passwordHash, nameof(passwordHash));
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public int FailedLoginAttemptCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = [];

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = RequireText(passwordHash, nameof(passwordHash));
    }

    public bool IsLockedOut(DateTimeOffset nowUtc)
    {
        return LockoutEndUtc > nowUtc.ToUniversalTime();
    }

    public void RegisterFailedLogin(
        DateTimeOffset nowUtc,
        int maximumAttempts,
        TimeSpan lockoutDuration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            lockoutDuration,
            TimeSpan.Zero);

        var utcNow = nowUtc.ToUniversalTime();
        if (LockoutEndUtc <= utcNow)
        {
            FailedLoginAttemptCount = 0;
            LockoutEndUtc = null;
        }

        FailedLoginAttemptCount++;

        if (FailedLoginAttemptCount >= maximumAttempts)
        {
            LockoutEndUtc = utcNow.Add(lockoutDuration);
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttemptCount = 0;
        LockoutEndUtc = null;
    }

    public void AssignRole(Guid roleId, DateTimeOffset assignedAtUtc)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        if (UserRoles.Any(userRole => userRole.RoleId == roleId))
        {
            return;
        }

        UserRoles.Add(new UserRole(Id, roleId, assignedAtUtc));
    }

    public void ReplaceRoles(
        IEnumerable<Guid> roleIds,
        DateTimeOffset assignedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var desiredRoleIds = roleIds.ToHashSet();

        if (desiredRoleIds.Count == 0
            || desiredRoleIds.Contains(Guid.Empty))
        {
            throw new ArgumentException(
                "At least one valid role is required.",
                nameof(roleIds));
        }

        foreach (var userRole in UserRoles
                     .Where(userRole =>
                         !desiredRoleIds.Contains(userRole.RoleId))
                     .ToArray())
        {
            UserRoles.Remove(userRole);
        }

        foreach (var roleId in desiredRoleIds)
        {
            AssignRole(roleId, assignedAtUtc);
        }
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
