using System.Net.Mail;
using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public sealed class AdminBootstrapService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    TimeProvider timeProvider)
{
    public async Task<BootstrapAdminResult> BootstrapAsync(
        BootstrapAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        Validate(command);

        if (await identityRepository.HasAdminAsync(cancellationToken))
        {
            return new BootstrapAdminResult(
                BootstrapAdminStatus.AlreadyConfigured,
                UserId: null);
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var existingUser =
            await identityRepository.FindUserByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "Bootstrap email is already assigned to a non-admin user.");
        }

        var adminRole =
            await identityRepository.FindRoleByNormalizedNameAsync(
                SystemRoleNames.Admin.ToUpperInvariant(),
                cancellationToken)
            ?? throw new InvalidOperationException(
                "The seeded Admin role could not be found.");

        var now = timeProvider.GetUtcNow();
        var user = new User(
            Guid.NewGuid(),
            command.Email,
            command.DisplayName,
            "bootstrap-placeholder",
            now);

        user.ChangePasswordHash(
            passwordHashService.HashPassword(user, command.Password));
        user.AssignRole(adminRole.Id, now);

        await identityRepository.AddUserAsync(user, cancellationToken);
        await identityRepository.SaveChangesAsync(cancellationToken);

        return new BootstrapAdminResult(
            BootstrapAdminStatus.Created,
            user.Id);
    }

    private static void Validate(BootstrapAdminCommand command)
    {
        if (!MailAddress.TryCreate(command.Email?.Trim(), out var address)
            || !string.Equals(
                address.Address,
                command.Email.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A valid bootstrap admin email is required.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName)
            || command.DisplayName.Trim().Length > 120)
        {
            throw new ArgumentException(
                "Bootstrap admin display name must contain at most 120 characters.",
                nameof(command));
        }

        ValidatePassword(command.Password);
    }

    private static void ValidatePassword(string password)
    {
        var isValid = !string.IsNullOrEmpty(password)
            && password.Length is >= 12 and <= 128
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character =>
                char.IsPunctuation(character) || char.IsSymbol(character));

        if (!isValid)
        {
            throw new ArgumentException(
                "Bootstrap password must be 12-128 characters and include upper-case, lower-case, number, and symbol characters.",
                nameof(password));
        }
    }
}
