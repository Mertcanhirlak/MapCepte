using System.Diagnostics.CodeAnalysis;

namespace Transport.Domain.Identity;

[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "RolePermission is the established domain term for the join entity.")]
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        if (permissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Permission id cannot be empty.",
                nameof(permissionId));
        }

        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Role Role { get; private set; } = null!;

    public Permission Permission { get; private set; } = null!;
}
