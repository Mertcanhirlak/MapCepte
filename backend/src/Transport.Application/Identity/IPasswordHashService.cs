using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    PasswordVerificationOutcome VerifyPassword(
        User user,
        string passwordHash,
        string providedPassword);

    void PerformDummyVerification(string providedPassword);
}
