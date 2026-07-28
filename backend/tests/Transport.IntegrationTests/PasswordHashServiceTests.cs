using Microsoft.AspNetCore.Identity;
using Transport.Application.Identity;
using Transport.Domain.Identity;
using Transport.Infrastructure.Identity;

namespace Transport.IntegrationTests;

public sealed class PasswordHashServiceTests
{
    [Fact]
    public void HashesAndVerifiesWithoutStoringPlainText()
    {
        var service = new AspNetPasswordHashService(new PasswordHasher<User>());
        var user = new User(
            Guid.NewGuid(),
            "admin@example.com",
            "Initial Admin",
            "placeholder",
            DateTimeOffset.UtcNow);

        var hash = service.HashPassword(user, "Strong-Password-2026!");

        Assert.NotEqual("Strong-Password-2026!", hash);
        Assert.Equal(
            PasswordVerificationOutcome.Success,
            service.VerifyPassword(
                user,
                hash,
                "Strong-Password-2026!"));
        Assert.Equal(
            PasswordVerificationOutcome.Failed,
            service.VerifyPassword(
                user,
                hash,
                "Wrong-Password-2026!"));
    }
}
