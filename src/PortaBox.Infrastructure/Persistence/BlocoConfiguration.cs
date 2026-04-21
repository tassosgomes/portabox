using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Infrastructure.Persistence;

public sealed class BlocoConfiguration : IEntityTypeConfiguration<Bloco>
{
    public void Configure(EntityTypeBuilder<Bloco> builder)
    {
        builder.ToTable("bloco");

        builder.HasKey(bloco => bloco.Id);

        builder.Property(bloco => bloco.Id)
            .ValueGeneratedNever();

        builder.Property(bloco => bloco.TenantId)
            .IsRequired();

        builder.Property(bloco => bloco.CondominioId)
            .IsRequired();

        builder.Property(bloco => bloco.Nome)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(bloco => bloco.Ativo)
            .IsRequired();

        builder.Property(bloco => bloco.InativadoEm);

        builder.Property(bloco => bloco.InativadoPor);

        builder.Property(bloco => bloco.CriadoEm)
            .IsRequired();

        builder.Property(bloco => bloco.CriadoPor)
            .IsRequired();

        builder.HasIndex(bloco => bloco.CondominioId)
            .HasDatabaseName("idx_bloco_condominio");

        builder.HasIndex(bloco => new { bloco.TenantId, bloco.CondominioId, bloco.Nome })
            .IsUnique()
            .HasFilter("ativo = true")
            .HasDatabaseName("idx_bloco_nome_ativo_unique");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(bloco => bloco.CriadoPor)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(bloco => bloco.InativadoPor)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PortaBox.Modules.Gestao.Domain.Condominio>()
            .WithMany()
            .HasForeignKey(bloco => bloco.CondominioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
