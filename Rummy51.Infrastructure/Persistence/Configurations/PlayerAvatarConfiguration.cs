using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class PlayerAvatarConfiguration : IEntityTypeConfiguration<PlayerAvatar>
{
    public void Configure(EntityTypeBuilder<PlayerAvatar> builder)
    {
        builder.ToTable("player_avatars");

        // The player's own id, so a player can hold at most one picture and
        // needs no second identifier for it.
        builder.HasKey(avatar => avatar.PlayerId);

        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerAvatar>(avatar => avatar.PlayerId)
            // Named here because Identity still calls the principal table by
            // its own name, which would read as asp_net_users in the schema.
            .HasConstraintName("fk_player_avatars_players_player_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(avatar => avatar.Image).IsRequired();

        builder.Property(avatar => avatar.ContentType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(avatar => avatar.UpdatedAt).IsRequired();
    }
}
