using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;
using Transport.Domain.TransitLines;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class TransitLineConfiguration
    : IEntityTypeConfiguration<TransitLine>
{
    public void Configure(EntityTypeBuilder<TransitLine> builder)
    {
        builder.ToTable("transit_lines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .HasColumnName("id");

        builder.Property(line => line.Name)
            .HasColumnName("name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(line => line.Code)
            .HasColumnName("code")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(line => line.NormalizedCode)
            .HasColumnName("normalized_code")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(line => line.NormalizedCode)
            .IsUnique()
            .HasDatabaseName("ux_transit_lines_normalized_code");

        builder.Property(line => line.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(line => line.Color)
            .HasColumnName("color")
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(line => line.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(line => line.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(line => line.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(line => line.UpdatedByUserId)
            .HasColumnName("updated_by_user_id")
            .IsRequired();

        builder.Property(line => line.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(line => line.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(line => line.Version)
            .HasColumnName("version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(line => line.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(line => line.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(line => line.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(line => line.Stops)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
