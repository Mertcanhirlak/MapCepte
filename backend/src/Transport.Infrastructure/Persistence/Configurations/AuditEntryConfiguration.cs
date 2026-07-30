using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transport.Domain.Identity;

namespace Transport.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration
    : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id");

        builder.Property(entry => entry.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entry => entry.Outcome)
            .HasColumnName("outcome")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(entry => entry.SubjectUserId)
            .HasColumnName("subject_user_id");

        builder.Property(entry => entry.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.HasIndex(entry => entry.OccurredAtUtc)
            .HasDatabaseName("ix_audit_entries_occurred_at_utc");

        builder.HasIndex(entry => entry.EventType)
            .HasDatabaseName("ix_audit_entries_event_type");
    }
}
