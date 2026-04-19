using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence;

public sealed class OptInDocumentConfiguration : IEntityTypeConfiguration<OptInDocument>
{
    public void Configure(EntityTypeBuilder<OptInDocument> builder)
    {
        builder.ToTable("opt_in_document");

        builder.HasKey(optInDocument => optInDocument.Id);

        builder.Property(optInDocument => optInDocument.Id)
            .ValueGeneratedNever();

        builder.Property(optInDocument => optInDocument.TenantId)
            .IsRequired();

        builder.Property(optInDocument => optInDocument.Kind)
            .HasConversion<short>()
            .IsRequired();

        builder.Property(optInDocument => optInDocument.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(optInDocument => optInDocument.ContentType)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(optInDocument => optInDocument.SizeBytes)
            .IsRequired();

        builder.Property(optInDocument => optInDocument.Sha256)
            .HasColumnType("character(64)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(optInDocument => optInDocument.UploadedAt)
            .IsRequired();

        builder.Property(optInDocument => optInDocument.UploadedByUserId)
            .IsRequired();

        builder.HasIndex(optInDocument => new { optInDocument.TenantId, optInDocument.UploadedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_opt_in_document_tenant_id_uploaded_at_desc");

        builder.HasOne(optInDocument => optInDocument.Condominio)
            .WithMany(condominio => condominio.OptInDocuments)
            .HasForeignKey(optInDocument => optInDocument.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(optInDocument => optInDocument.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
