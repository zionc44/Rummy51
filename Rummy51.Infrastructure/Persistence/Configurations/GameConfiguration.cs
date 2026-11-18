using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.HasKey(game => game.Id);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(game => game.WinnerId)
            .HasConstraintName("fk_games_players_winner_id")
            // A player is not deleted today, and if one ever is, the games
            // they won are not theirs to take with them.
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(game => game.Mode).IsRequired();
        builder.Property(game => game.Seats).IsRequired();
        builder.Property(game => game.Jokers).IsRequired();
        builder.Property(game => game.StartedAt).IsRequired();

        builder.HasIndex(game => game.StartedAt).HasDatabaseName("ix_games_started_at");
    }
}
