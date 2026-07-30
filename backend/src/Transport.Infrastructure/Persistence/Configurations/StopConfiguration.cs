using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;
using Transport.Domain.Stops;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("stops");
        builder.HasKey(stop => stop.Id);

        builder.Property(stop => stop.Id)
            .HasColumnName("id");

        builder.Property(stop => stop.Name)
            .HasColumnName("name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(stop => stop.Code)
            .HasColumnName("code")
            .HasMaxLength(40);

        builder.Property(stop => stop.NormalizedCode)
            .HasColumnName("normalized_code")
            .HasMaxLength(40);

        builder.HasIndex(stop => stop.NormalizedCode)
            .IsUnique()
            .HasFilter("normalized_code IS NOT NULL")
            .HasDatabaseName("ux_stops_normalized_code");

        builder.Property(stop => stop.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(stop => stop.Color)
            .HasColumnName("color")
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(stop => stop.Location)
            .HasColumnName("location")
            .HasColumnType("geography (point, 4326)")
            .IsRequired();

        builder.HasIndex(stop => stop.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_stops_location");

        builder.Property(stop => stop.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(stop => stop.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(stop => stop.UpdatedByUserId)
            .HasColumnName("updated_by_user_id")
            .IsRequired();

        builder.Property(stop => stop.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(stop => stop.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(stop => stop.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(stop => stop.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
