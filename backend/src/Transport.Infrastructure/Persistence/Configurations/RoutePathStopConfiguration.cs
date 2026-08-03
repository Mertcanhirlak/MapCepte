using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.RoutePaths;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class RoutePathStopConfiguration : IEntityTypeConfiguration<RoutePathStop>
{
    public void Configure(EntityTypeBuilder<RoutePathStop> builder)
    {
        builder.ToTable("route_path_stops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Location)
            .HasColumnType("geography(Point,4326)")
            .IsRequired();

        builder.HasIndex(s => new { s.RoutePathId, s.Sequence }).IsUnique();
    }
}
