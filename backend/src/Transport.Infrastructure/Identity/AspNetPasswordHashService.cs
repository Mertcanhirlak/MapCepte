using Microsoft.AspNetCore.Identity;
using Transport.Application.Identity;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Identity;

public sealed class AspNetPasswordHashService(
    IPasswordHasher<User> passwordHasher)
    : IPasswordHashService
{
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
}
