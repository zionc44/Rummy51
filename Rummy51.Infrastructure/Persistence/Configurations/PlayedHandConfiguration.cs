using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class PlayedHandConfiguration : IEntityTypeConfiguration<PlayedHand>
{
    /// <summary>Two packs and four jokers, by code, comma separated.</summary>
    private const int DeckOrderLength = 600;

    public void Configure(EntityTypeBuilder<PlayedHand> builder)
    {
        builder.ToTable("played_hands");

        builder.HasKey(hand => hand.Id);

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(hand => hand.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(hand => hand.Number).IsRequired();
        builder.Property(hand => hand.DealerSeat).IsRequired();
        builder.Property(hand => hand.CutAt).IsRequired();
        builder.Property(hand => hand.Seed).IsRequired();
        builder.Property(hand => hand.StartedAt).IsRequired();

        builder.Property(hand => hand.FinishingCard).HasMaxLength(8);

        builder.Property(hand => hand.DeckOrder)
            .HasMaxLength(DeckOrderLength)
            .IsRequired();

        builder.HasIndex(hand => new { hand.GameId, hand.Number })
            .IsUnique()
            .HasDatabaseName("ix_played_hands_game_id_number");
    }
}
