using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Infrastructure.Persistence;

public sealed class UnidadeConfiguration : IEntityTypeConfiguration<Unidade>
{
    public void Configure(EntityTypeBuilder<Unidade> builder)
    {
        builder.ToTable("unidade", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_unidade_andar_non_negative", "andar >= 0");
        });

        builder.HasKey(unidade => unidade.Id);

        builder.Property(unidade => unidade.Id)
            .ValueGeneratedNever();

        builder.Property(unidade => unidade.TenantId)
            .IsRequired();

        builder.Property(unidade => unidade.BlocoId)
            .IsRequired();

        builder.Property(unidade => unidade.Andar)
            .IsRequired();

        builder.Property(unidade => unidade.Numero)
            .HasColumnType("varchar(5)")
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(unidade => unidade.Ativo)
            .IsRequired();

        builder.Property(unidade => unidade.InativadoEm);

        builder.Property(unidade => unidade.InativadoPor);

        builder.Property(unidade => unidade.CriadoEm)
            .IsRequired();

        builder.Property(unidade => unidade.CriadoPor)
            .IsRequired();

        builder.HasIndex(unidade => unidade.BlocoId)
            .HasDatabaseName("idx_unidade_bloco");

        builder.HasIndex(unidade => new { unidade.TenantId, unidade.BlocoId, unidade.Andar, unidade.Numero })
            .IsUnique()
            .HasFilter("ativo = true")
            .HasDatabaseName("idx_unidade_canonica_ativa");

        builder.HasOne<Bloco>()
            .WithMany()
            .HasForeignKey(unidade => unidade.BlocoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(unidade => unidade.CriadoPor)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(unidade => unidade.InativadoPor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
