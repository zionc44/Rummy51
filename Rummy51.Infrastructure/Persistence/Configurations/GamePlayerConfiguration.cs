using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("game_players");

        // The pair is the key, so one player sits once in one game and needs
        // no identifier of their own to say so.
        builder.HasKey(seated => new { seated.GameId, seated.PlayerId });

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(seated => seated.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(seated => seated.PlayerId)
            .HasConstraintName("fk_game_players_players_player_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(seated => seated.Seat).IsRequired();

        // Rule 10.4: one chair, one player, for as long as the game runs.
        builder.HasIndex(seated => new { seated.GameId, seated.Seat })
            .IsUnique()
            .HasDatabaseName("ix_game_players_game_id_seat");
    }
}
