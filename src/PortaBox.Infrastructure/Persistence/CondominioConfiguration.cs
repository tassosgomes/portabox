using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence;

public sealed class CondominioConfiguration : IEntityTypeConfiguration<Condominio>
{
    public void Configure(EntityTypeBuilder<Condominio> builder)
    {
        builder.ToTable("condominio");

        builder.HasKey(condominio => condominio.Id);

        builder.Property(condominio => condominio.Id)
            .ValueGeneratedNever();

        builder.Property(condominio => condominio.NomeFantasia)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(condominio => condominio.Cnpj)
            .HasColumnType("character(14)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(condominio => condominio.EnderecoLogradouro)
            .HasMaxLength(200);

        builder.Property(condominio => condominio.EnderecoNumero)
            .HasMaxLength(20);

        builder.Property(condominio => condominio.EnderecoComplemento)
            .HasMaxLength(80);

        builder.Property(condominio => condominio.EnderecoBairro)
            .HasMaxLength(80);

        builder.Property(condominio => condominio.EnderecoCidade)
            .HasMaxLength(80);

        builder.Property(condominio => condominio.EnderecoUf)
            .HasColumnType("character(2)")
            .IsFixedLength();

        builder.Property(condominio => condominio.EnderecoCep)
            .HasColumnType("character(8)")
            .IsFixedLength();

        builder.Property(condominio => condominio.AdministradoraNome)
            .HasMaxLength(200);

        builder.Property(condominio => condominio.Status)
            .HasConversion<short>()
            .IsRequired();

        builder.Property(condominio => condominio.CreatedAt)
            .IsRequired();

        builder.Property(condominio => condominio.CreatedByUserId)
            .IsRequired();

        builder.Property(condominio => condominio.ActivatedAt);

        builder.Property(condominio => condominio.ActivatedByUserId);

        builder.HasIndex(condominio => condominio.Cnpj)
            .IsUnique()
            .HasDatabaseName("idx_condominio_cnpj_unique");

        builder.HasIndex(condominio => condominio.Status)
            .HasDatabaseName("idx_condominio_status");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(condominio => condominio.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(condominio => condominio.ActivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(condominio => condominio.OptInRecord)
            .WithOne(optInRecord => optInRecord.Condominio)
            .HasForeignKey<OptInRecord>(optInRecord => optInRecord.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(condominio => condominio.OptInDocuments)
            .WithOne(optInDocument => optInDocument.Condominio)
            .HasForeignKey(optInDocument => optInDocument.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(condominio => condominio.Sindicos)
            .WithOne(sindico => sindico.Condominio)
            .HasForeignKey(sindico => sindico.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(condominio => condominio.TenantAuditEntries)
            .WithOne(entry => entry.Condominio)
            .HasForeignKey(entry => entry.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
