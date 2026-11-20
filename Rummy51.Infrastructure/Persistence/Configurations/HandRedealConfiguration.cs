using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class HandRedealConfiguration : IEntityTypeConfiguration<HandRedeal>
{
    /// <summary>Eight cards by code, comma separated, and room to spare.</summary>
    private const int ForcedWithLength = 60;

    public void Configure(EntityTypeBuilder<HandRedeal> builder)
    {
        builder.ToTable("hand_redeals");

        builder.HasKey(redeal => redeal.Id);

        builder.HasOne<PlayedHand>()
            .WithMany()
            .HasForeignKey(redeal => redeal.PlayedHandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(redeal => redeal.OfferedById)
            .HasConstraintName("fk_hand_redeals_players_offered_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(redeal => redeal.ForcedWith)
            .HasMaxLength(ForcedWithLength)
            .IsRequired();

        builder.Property(redeal => redeal.Number).IsRequired();
        builder.Property(redeal => redeal.Forced).IsRequired();
        builder.Property(redeal => redeal.AtOnce).IsRequired();
        builder.Property(redeal => redeal.Agreed).IsRequired();

        // One place in the order is one redeal, however many times anything
        // is retried.
        builder.HasIndex(redeal => new { redeal.PlayedHandId, redeal.Number })
            .IsUnique()
            .HasDatabaseName("ix_hand_redeals_played_hand_id_number");

        builder.HasIndex(redeal => redeal.OfferedById)
            .HasDatabaseName("ix_hand_redeals_offered_by_id");
    }
}
