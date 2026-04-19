using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Persistence;

public sealed class OptInRecordConfiguration : IEntityTypeConfiguration<OptInRecord>
{
    public void Configure(EntityTypeBuilder<OptInRecord> builder)
    {
        builder.ToTable("opt_in_record");

        builder.HasKey(optInRecord => optInRecord.Id);

        builder.Property(optInRecord => optInRecord.Id)
            .ValueGeneratedNever();

        builder.Property(optInRecord => optInRecord.TenantId)
            .IsRequired();

        builder.Property(optInRecord => optInRecord.DataAssembleia)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(optInRecord => optInRecord.QuorumDescricao)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(optInRecord => optInRecord.SignatarioNome)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(optInRecord => optInRecord.SignatarioCpf)
            .HasColumnType("character(11)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(optInRecord => optInRecord.DataTermo)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(optInRecord => optInRecord.RegisteredByUserId)
            .IsRequired();

        builder.Property(optInRecord => optInRecord.RegisteredAt)
            .IsRequired();

        builder.HasIndex(optInRecord => optInRecord.TenantId)
            .IsUnique()
            .HasDatabaseName("idx_opt_in_record_tenant_id_unique");

        builder.HasOne(optInRecord => optInRecord.Condominio)
            .WithOne(condominio => condominio.OptInRecord)
            .HasForeignKey<OptInRecord>(optInRecord => optInRecord.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(optInRecord => optInRecord.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
