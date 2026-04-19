using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence.Configurations;

public sealed class TenantAuditEntryConfiguration : IEntityTypeConfiguration<TenantAuditEntry>
{
    public void Configure(EntityTypeBuilder<TenantAuditEntry> builder)
    {
        builder.ToTable("tenant_audit_log");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedOnAdd();

        builder.Property(entry => entry.TenantId)
            .IsRequired();

        builder.Property(entry => entry.EventKind)
            .HasConversion<short>()
            .IsRequired();

        builder.Property(entry => entry.PerformedByUserId)
            .IsRequired();

        builder.Property(entry => entry.OccurredAt)
            .IsRequired();

        builder.Property(entry => entry.Note);

        builder.Property(entry => entry.MetadataJson)
            .HasColumnType("jsonb");

        builder.HasOne(entry => entry.Condominio)
            .WithMany(condominio => condominio.TenantAuditEntries)
            .HasForeignKey(entry => entry.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(entry => entry.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
