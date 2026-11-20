using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class HandResultConfiguration : IEntityTypeConfiguration<HandResult>
{
    /// <summary>Fifteen cards by code, comma separated, and room to spare.</summary>
    private const int HeldCardsLength = 100;

    public void Configure(EntityTypeBuilder<HandResult> builder)
    {
        builder.ToTable("hand_results");

        // The pair is the key, so the same hand cannot be scored twice for
        // the same player however many times anything is retried.
        builder.HasKey(result => new { result.PlayedHandId, result.PlayerId });

        builder.HasOne<PlayedHand>()
            .WithMany()
            .HasForeignKey(result => result.PlayedHandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(result => result.PlayerId)
            .HasConstraintName("fk_hand_results_players_player_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(result => result.HeldCards)
            .HasMaxLength(HeldCardsLength)
            .IsRequired();

        builder.Property(result => result.CardsLeft).IsRequired();
        builder.Property(result => result.HeldWorth).IsRequired();
        builder.Property(result => result.ResultMultiplier).IsRequired();
        builder.Property(result => result.Score).IsRequired();

        // Everything anybody will ask of this table starts with a player.
        builder.HasIndex(result => result.PlayerId).HasDatabaseName("ix_hand_results_player_id");
    }
}
