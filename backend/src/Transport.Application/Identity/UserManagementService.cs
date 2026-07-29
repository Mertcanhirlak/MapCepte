using System.Net.Mail;
using Transport.Domain.Identity;

namespace Transport.Application.Identity;

public sealed class UserManagementService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyCollection<UserCatalogItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return identityRepository.ListUsersAsync(cancellationToken);
    }

    public async Task<UserManagementResult> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationError = ValidateCreate(command);
        if (validationError is not null)
        {
            return Invalid(validationError);
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var existingUser =
            await identityRepository.FindUserByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (existingUser is not null)
        {
            return new UserManagementResult(
                UserManagementStatus.DuplicateEmail,
                Error: "A user with this email already exists.");
        }

        var roles = await ResolveRolesAsync(
            command.Roles,
            cancellationToken);

        if (roles is null)
        {
            return new UserManagementResult(
                UserManagementStatus.UnknownRole,
                Error: "One or more roles are unknown.");
        }

        var now = timeProvider.GetUtcNow();
        var user = new User(
            Guid.NewGuid(),
            command.Email,
            command.DisplayName,
            "user-management-placeholder",
            now);

        user.ChangePasswordHash(
            passwordHashService.HashPassword(user, command.Password));

        foreach (var role in roles)
        {
            user.AssignRole(role.Id, now);
        }

        await identityRepository.AddUserAsync(user, cancellationToken);
        await identityRepository.SaveChangesAsync(cancellationToken);

        return Success(user, roles);
    }

    public async Task<UserManagementResult> UpdateRolesAsync(
        UpdateUserRolesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ActorUserId == Guid.Empty
            || command.UserId == Guid.Empty)
        {
            return Invalid("A valid actor and target user are required.");
        }

        if (command.ActorUserId == command.UserId)
        {
            return new UserManagementResult(
                UserManagementStatus.SelfRoleChangeForbidden,
                Error: "Users cannot change their own roles.");
        }

        var roleError = ValidateRoles(command.Roles);
        if (roleError is not null)
        {
            return Invalid(roleError);
        }

        var user = await identityRepository.FindUserByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null)
        {
            return new UserManagementResult(
                UserManagementStatus.UserNotFound,
                Error: "User was not found.");
        }

        var roles = await ResolveRolesAsync(
            command.Roles,
            cancellationToken);

        if (roles is null)
        {
            return new UserManagementResult(
                UserManagementStatus.UnknownRole,
                Error: "One or more roles are unknown.");
        }

        user.ReplaceRoles(
            roles.Select(role => role.Id),
            timeProvider.GetUtcNow());
        await identityRepository.SaveChangesAsync(cancellationToken);

        return Success(user, roles);
    }

    private async Task<IReadOnlyCollection<Role>?> ResolveRolesAsync(
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken)
    {
        var roles = new List<Role>();

        foreach (var roleName in roleNames
                     .Select(role => role.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role =
                await identityRepository.FindRoleByNormalizedNameAsync(
                    roleName.ToUpperInvariant(),
                    cancellationToken);

            if (role is null)
            {
                return null;
            }

            roles.Add(role);
        }

        return roles;
    }

    private static string? ValidateCreate(CreateUserCommand command)
    {
        var email = command.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 320
            || !MailAddress.TryCreate(email, out var address)
            || !string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            return "A valid email address is required.";
        }

        if (string.IsNullOrWhiteSpace(command.DisplayName)
            || command.DisplayName.Trim().Length > 120)
        {
            return "Display name must contain at most 120 characters.";
        }

        if (!IsPasswordValid(
                command.Password,
                command.AllowWeakPassword))
        {
            return "Password does not satisfy the active password policy.";
        }

        return ValidateRoles(command.Roles);
    }

    private static string? ValidateRoles(
        IReadOnlyCollection<string>? roleNames)
    {
        if (roleNames is null
            || roleNames.Count == 0
            || roleNames.Any(string.IsNullOrWhiteSpace))
        {
            return "At least one role is required.";
        }

        return null;
    }

    private static bool IsPasswordValid(
        string? password,
        bool allowWeakPassword)
    {
        if (allowWeakPassword)
        {
            return !string.IsNullOrWhiteSpace(password)
                && password.Length is >= 6 and <= 128;
        }

        return !string.IsNullOrEmpty(password)
            && password.Length is >= 12 and <= 128
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character =>
                char.IsPunctuation(character) || char.IsSymbol(character));
    }

    private static UserManagementResult Invalid(string error)
    {
        return new UserManagementResult(
            UserManagementStatus.InvalidInput,
            Error: error);
    }

    private static UserManagementResult Success(
        User user,
        IReadOnlyCollection<Role> roles)
    {
        return new UserManagementResult(
            UserManagementStatus.Success,
            new UserCatalogItem(
                user.Id,
                user.Email,
                user.DisplayName,
                user.IsActive,
                user.CreatedAtUtc,
                roles.Select(role => role.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray()));
    }
}
