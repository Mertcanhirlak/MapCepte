using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Stops;
using Transport.Domain.TransitLines;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class TransitLineStopConfiguration
    : IEntityTypeConfiguration<TransitLineStop>
{
    public void Configure(EntityTypeBuilder<TransitLineStop> builder)
    {
        builder.ToTable("transit_line_stops");
        builder.HasKey(lineStop => lineStop.Id);

        builder.Property(lineStop => lineStop.Id)
            .HasColumnName("id");

        builder.Property(lineStop => lineStop.TransitLineId)
            .HasColumnName("transit_line_id")
            .IsRequired();

        builder.Property(lineStop => lineStop.StopId)
            .HasColumnName("stop_id")
            .IsRequired();

        builder.Property(lineStop => lineStop.Sequence)
            .HasColumnName("sequence")
            .IsRequired();

        builder.Property(lineStop => lineStop.BoardingAllowed)
            .HasColumnName("boarding_allowed")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(lineStop => lineStop.AlightingAllowed)
            .HasColumnName("alighting_allowed")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(lineStop => lineStop.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(lineStop => new
        {
            lineStop.TransitLineId,
            lineStop.StopId,
        })
            .IsUnique()
            .HasDatabaseName("ux_transit_line_stops_line_stop");

        builder.HasIndex(lineStop => new
        {
            lineStop.TransitLineId,
            lineStop.Sequence,
        })
            .IsUnique()
            .HasDatabaseName("ux_transit_line_stops_line_sequence");

        builder.HasIndex(lineStop => lineStop.StopId)
            .HasDatabaseName("ix_transit_line_stops_stop_id");

        builder.HasOne<TransitLine>()
            .WithMany(line => line.Stops)
            .HasForeignKey(lineStop => lineStop.TransitLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stop>()
            .WithMany()
            .HasForeignKey(lineStop => lineStop.StopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
