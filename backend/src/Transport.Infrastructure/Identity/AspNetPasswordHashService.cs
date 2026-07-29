using Microsoft.AspNetCore.Identity;
using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Identity;

public sealed class AspNetPasswordHashService(
    IPasswordHasher<User> passwordHasher)
    : IPasswordHashService
{
    private static readonly User DummyUser = new(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        "dummy@invalid.local",
        "Dummy User",
        "dummy-placeholder",
        DateTimeOffset.UnixEpoch);

    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(
            DummyUser,
            "Dummy-Password-For-Timing-Only-2026!");

    public string HashPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));
        }

        return passwordHasher.HashPassword(user, password);
    }

    public PasswordVerificationOutcome VerifyPassword(
        User user,
        string passwordHash,
        string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(user);

        var result = passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            providedPassword);

        return result switch
        {
            PasswordVerificationResult.Success =>
                PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded =>
                PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed,
        };
    }

    public void PerformDummyVerification(string providedPassword)
    {
        passwordHasher.VerifyHashedPassword(
            DummyUser,
            DummyPasswordHash,
            providedPassword);
    }
}
