using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.MagicLinks;

namespace PortaBox.Infrastructure.Persistence.Configurations;

public sealed class MagicLinkConfiguration : IEntityTypeConfiguration<MagicLink>
{
    public void Configure(EntityTypeBuilder<MagicLink> builder)
    {
        builder.ToTable("magic_link");

        builder.HasKey(magicLink => magicLink.Id);

        builder.Property(magicLink => magicLink.Id)
            .ValueGeneratedNever();

        builder.Property(magicLink => magicLink.UserId)
            .IsRequired();

        builder.Property(magicLink => magicLink.Purpose)
            .HasConversion<short>()
            .IsRequired();

        builder.Property(magicLink => magicLink.TokenHash)
            .HasColumnType("character(64)")
            .IsFixedLength()
            .IsRequired();

        builder.Property(magicLink => magicLink.CreatedAt)
            .IsRequired();

        builder.Property(magicLink => magicLink.ExpiresAt)
            .IsRequired();

        builder.Property(magicLink => magicLink.ConsumedAt);

        builder.Property(magicLink => magicLink.ConsumedByIp)
            .HasColumnType("inet");

        builder.Property(magicLink => magicLink.InvalidatedAt);

        builder.HasIndex(magicLink => magicLink.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_magic_link_token_hash_unique");

        builder.HasIndex(magicLink => new { magicLink.UserId, magicLink.Purpose })
            .HasDatabaseName("idx_magic_link_user_purpose_open")
            .HasFilter("consumed_at IS NULL AND invalidated_at IS NULL");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(magicLink => magicLink.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
