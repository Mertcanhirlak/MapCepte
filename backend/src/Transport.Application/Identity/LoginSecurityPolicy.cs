namespace Transport.Application.Identity;

public sealed record LoginSecurityPolicy(
    int MaximumFailedAttempts,
    TimeSpan LockoutDuration)
{
    public LoginSecurityPolicy()
        : this(5, TimeSpan.FromMinutes(15))
    {
    }
}
