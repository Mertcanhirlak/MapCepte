namespace Transport.Application.Identity;

public sealed class LoginService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService)
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
            return InvalidCredentials();
        }

        var authenticationData =
            await identityRepository.FindUserAuthenticationDataAsync(
                command.Email.Trim().ToUpperInvariant(),
                cancellationToken);

        if (authenticationData is null)
        {
            passwordHashService.PerformDummyVerification(command.Password);
            return InvalidCredentials();
        }

        var verification = passwordHashService.VerifyPassword(
            authenticationData.User,
            authenticationData.User.PasswordHash,
            command.Password);

        if (verification == PasswordVerificationOutcome.Failed
            || !authenticationData.User.IsActive)
        {
            return InvalidCredentials();
        }

        if (verification == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            authenticationData.User.ChangePasswordHash(
                passwordHashService.HashPassword(
                    authenticationData.User,
                    command.Password));
            await identityRepository.SaveChangesAsync(cancellationToken);
        }

        return new LoginResult(
            LoginStatus.Success,
            new AuthenticatedUser(
                authenticationData.User.Id,
                authenticationData.User.Email,
                authenticationData.User.DisplayName,
                authenticationData.Roles,
                authenticationData.Permissions));
    }

    private static LoginResult InvalidCredentials()
    {
        return new LoginResult(LoginStatus.InvalidCredentials, User: null);
    }
}
