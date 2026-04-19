using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Events;

namespace PortaBox.Infrastructure.Persistence.Configurations;

public sealed class DomainEventOutboxEntryConfiguration : IEntityTypeConfiguration<DomainEventOutboxEntry>
{
    public void Configure(EntityTypeBuilder<DomainEventOutboxEntry> builder)
    {
        builder.ToTable("domain_event_outbox");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.TenantId);

        builder.Property(entry => entry.EventType)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(entry => entry.AggregateId)
            .IsRequired();

        builder.Property(entry => entry.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(entry => entry.CreatedAt)
            .IsRequired();

        builder.Property(entry => entry.PublishedAt);

        builder.HasIndex(entry => new { entry.PublishedAt, entry.CreatedAt })
            .HasDatabaseName("idx_domain_event_outbox_published_at_created_at");
    }
}
