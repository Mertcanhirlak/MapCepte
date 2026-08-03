using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Trips;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TripCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Direction)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(t => t.TransitLineId);
        builder.HasIndex(t => t.OperatingCalendarId);

        builder.HasMany(t => t.StopTimes)
            .WithOne()
            .HasForeignKey(st => st.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
