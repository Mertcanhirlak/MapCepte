using System.Diagnostics.CodeAnalysis;

namespace Transport.Domain.Identity;

[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Permission is the established domain term for an authorization capability.")]
public sealed class Permission
{
    private Permission()
    {
    }

    public Permission(Guid id, string code, string description)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Permission id cannot be empty.",
                nameof(id));
        }

        Id = id;
        Code = RequireText(code, nameof(code)).ToLowerInvariant();
        Description = RequireText(description, nameof(description));
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; } = [];

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
