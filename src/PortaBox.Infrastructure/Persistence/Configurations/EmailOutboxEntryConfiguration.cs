using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Email;

namespace PortaBox.Infrastructure.Persistence.Configurations;

public sealed class EmailOutboxEntryConfiguration : IEntityTypeConfiguration<EmailOutboxEntry>
{
    public void Configure(EntityTypeBuilder<EmailOutboxEntry> builder)
    {
        builder.ToTable("email_outbox");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.ToAddress)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(entry => entry.Subject)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entry => entry.HtmlBody)
            .IsRequired();

        builder.Property(entry => entry.TextBody);

        builder.Property(entry => entry.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(entry => entry.NextAttemptAt)
            .IsRequired();

        builder.Property(entry => entry.LastError);

        builder.Property(entry => entry.SentAt);

        builder.HasIndex(entry => new { entry.SentAt, entry.NextAttemptAt })
            .HasDatabaseName("idx_email_outbox_sent_at_next_attempt_at");
    }
}
