using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.RoutePaths;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class RoutePathConfiguration : IEntityTypeConfiguration<RoutePath>
{
    public void Configure(EntityTypeBuilder<RoutePath> builder)
    {
        builder.ToTable("route_paths");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Direction)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.ColorOverride)
            .HasMaxLength(7);

        builder.Property(r => r.Geometry)
            .HasColumnType("geography(LineString,4326)");

        builder.Property(r => r.RoutingEngine)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.InputHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.FailureCode)
            .HasMaxLength(50);

        builder.Property(r => r.FailureMessage)
            .HasMaxLength(500);

        builder.HasIndex(r => r.TransitLineId);
        builder.HasIndex(r => new { r.TransitLineId, r.Version }).IsUnique();

        builder.HasMany(r => r.Stops)
            .WithOne()
            .HasForeignKey(s => s.RoutePathId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
