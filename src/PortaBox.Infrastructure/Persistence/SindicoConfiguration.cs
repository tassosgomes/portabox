using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence;

public sealed class SindicoConfiguration : IEntityTypeConfiguration<Sindico>
{
    public void Configure(EntityTypeBuilder<Sindico> builder)
    {
        builder.ToTable("sindico");

        builder.HasKey(sindico => sindico.Id);

        builder.Property(sindico => sindico.Id)
            .ValueGeneratedNever();

        builder.Property(sindico => sindico.TenantId)
            .IsRequired();

        builder.Property(sindico => sindico.UserId)
            .IsRequired();

        builder.Property(sindico => sindico.NomeCompleto)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sindico => sindico.CelularE164)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sindico => sindico.Status)
            .HasConversion<short>()
            .IsRequired();

        builder.Property(sindico => sindico.CreatedAt)
            .IsRequired();

        builder.HasIndex(sindico => sindico.TenantId)
            .HasDatabaseName("idx_sindico_tenant_id");

        builder.HasIndex(sindico => sindico.UserId)
            .IsUnique()
            .HasDatabaseName("ix_sindico_user_id");

        builder.HasOne(sindico => sindico.Condominio)
            .WithMany(condominio => condominio.Sindicos)
            .HasForeignKey(sindico => sindico.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(sindico => sindico.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
