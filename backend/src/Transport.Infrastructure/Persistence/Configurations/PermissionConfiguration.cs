using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id");

        builder.Property(permission => permission.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique()
            .HasDatabaseName("ux_permissions_code");

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(240)
            .IsRequired();
    }
}
