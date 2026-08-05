using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Vehicles;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class VehiclePositionConfiguration : IEntityTypeConfiguration<VehiclePosition>
{
    public void Configure(EntityTypeBuilder<VehiclePosition> builder)
    {
        builder.ToTable("vehicle_positions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VehicleCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Location)
            .HasColumnType("geometry(Point, 4326)")
            .IsRequired();

        builder.HasIndex(v => v.TransitLineId);
        builder.HasIndex(v => v.RecordedAtUtc);
    }
}
