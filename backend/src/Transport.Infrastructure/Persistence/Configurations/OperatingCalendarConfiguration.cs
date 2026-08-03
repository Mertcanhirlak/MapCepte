using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Calendars;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class OperatingCalendarConfiguration : IEntityTypeConfiguration<OperatingCalendar>
{
    public void Configure(EntityTypeBuilder<OperatingCalendar> builder)
    {
        builder.ToTable("operating_calendars");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DaysOfWeek)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();
    }
}
