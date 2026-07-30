using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public sealed class LoginService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    IAuditStore auditStore,
    LoginSecurityPolicy securityPolicy,
    TimeProvider timeProvider)
{
    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email)
            || command.Email.Length > 320
            || string.IsNullOrEmpty(command.Password)
            || command.Password.Length > 128)
        {
            passwordHashService.PerformDummyVerification(
                command.Password ?? string.Empty);
            await RecordAndSaveAsync(
                AuditOutcomes.Failed,
                subjectUserId: null,
                command.IpAddress,
                cancellationToken);
            return InvalidCredentials();
        }

        var authenticationData =
            await identityRepository.FindUserAuthenticationDataAsync(
                command.Email.Trim().ToUpperInvariant(),
                cancellationToken);

        if (authenticationData is null)
        {
            passwordHashService.PerformDummyVerification(command.Password);
            await RecordAndSaveAsync(
                AuditOutcomes.Failed,
                subjectUserId: null,
                command.IpAddress,
                cancellationToken);
            return InvalidCredentials();
        }

        var user = authenticationData.User;
        var now = timeProvider.GetUtcNow();

        if (user.IsLockedOut(now))
        {
            await RecordAndSaveAsync(
                AuditOutcomes.LockedOut,
                user.Id,
                command.IpAddress,
                cancellationToken);
            return new LoginResult(LoginStatus.LockedOut, User: null);
        }

        var verification = passwordHashService.VerifyPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verification == PasswordVerificationOutcome.Failed
            || !user.IsActive)
        {
            var outcome = AuditOutcomes.Failed;

            if (user.IsActive)
            {
                user.RegisterFailedLogin(
                    now,
                    securityPolicy.MaximumFailedAttempts,
                    securityPolicy.LockoutDuration);

                if (user.IsLockedOut(now))
                {
                    outcome = AuditOutcomes.LockedOut;
                }
            }

            await RecordAndSaveAsync(
                outcome,
                user.Id,
                command.IpAddress,
                cancellationToken);

            return new LoginResult(
                outcome == AuditOutcomes.LockedOut
                    ? LoginStatus.LockedOut
                    : LoginStatus.InvalidCredentials,
                User: null);
        }

        if (verification == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(
                passwordHashService.HashPassword(user, command.Password));
        }

        user.RegisterSuccessfulLogin();
        await RecordAndSaveAsync(
            AuditOutcomes.Succeeded,
            user.Id,
            command.IpAddress,
            cancellationToken);

        return new LoginResult(
            LoginStatus.Success,
            new AuthenticatedUser(
                user.Id,
                user.Email,
                user.DisplayName,
                authenticationData.Roles,
                authenticationData.Permissions));
    }

    private async Task RecordAndSaveAsync(
        string outcome,
        Guid? subjectUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await auditStore.AddAsync(
            new AuditEntry(
                Guid.NewGuid(),
                AuditEventNames.Login,
                outcome,
                timeProvider.GetUtcNow(),
                actorUserId: subjectUserId,
                subjectUserId,
                ipAddress),
            cancellationToken);
        await identityRepository.SaveChangesAsync(cancellationToken);
    }

    private static LoginResult InvalidCredentials()
    {
        return new LoginResult(LoginStatus.InvalidCredentials, User: null);
    }
}
