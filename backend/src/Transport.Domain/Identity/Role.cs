namespace Transport.Domain.Identity;

public sealed class Role
{
    private Role()
    {
    }

    public Role(Guid id, string name, string description, bool isSystem)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(id));
        }

        Id = id;
        Name = RequireText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        Description = RequireText(description, nameof(description));
        IsSystem = isSystem;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = [];

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
