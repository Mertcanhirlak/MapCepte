using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration
    : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rolePermission =>
            new { rolePermission.RoleId, rolePermission.PermissionId });

        builder.Property(rolePermission => rolePermission.RoleId)
            .HasColumnName("role_id");

        builder.Property(rolePermission => rolePermission.PermissionId)
            .HasColumnName("permission_id");

        builder.HasOne(rolePermission => rolePermission.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
