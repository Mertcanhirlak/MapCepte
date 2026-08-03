using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Trips;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class TripStopTimeConfiguration : IEntityTypeConfiguration<TripStopTime>
{
    public void Configure(EntityTypeBuilder<TripStopTime> builder)
    {
        builder.ToTable("trip_stop_times");

        builder.HasKey(st => st.Id);

        builder.HasIndex(st => new { st.TripId, st.Sequence }).IsUnique();
        builder.HasIndex(st => st.StopId);
    }
}
