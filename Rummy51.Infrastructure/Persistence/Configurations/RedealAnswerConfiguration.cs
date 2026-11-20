using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class RedealAnswerConfiguration : IEntityTypeConfiguration<RedealAnswer>
{
    public void Configure(EntityTypeBuilder<RedealAnswer> builder)
    {
        builder.ToTable("redeal_answers");

        builder.HasKey(answer => new { answer.HandRedealId, answer.PlayerId });

        builder.HasOne<HandRedeal>()
            .WithMany()
            .HasForeignKey(answer => answer.HandRedealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(answer => answer.PlayerId)
            .HasConstraintName("fk_redeal_answers_players_player_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(answer => answer.Reply).IsRequired();

        // Everything anybody will ask of this table starts with a player.
        builder.HasIndex(answer => answer.PlayerId)
            .HasDatabaseName("ix_redeal_answers_player_id");
    }
}
