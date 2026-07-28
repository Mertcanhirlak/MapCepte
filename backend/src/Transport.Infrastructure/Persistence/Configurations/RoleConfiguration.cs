using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .HasColumnName("id");

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ux_roles_normalized_name");

        builder.Property(role => role.Description)
            .HasColumnName("description")
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(role => role.IsSystem)
            .HasColumnName("is_system")
            .IsRequired();
    }
}
