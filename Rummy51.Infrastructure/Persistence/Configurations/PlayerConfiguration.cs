using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.Property(p => p.DisplayName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Identity names these two indexes explicitly, so the snake_case
        // convention leaves them alone; rename them to match the schema.
        builder.HasIndex(p => p.NormalizedUserName)
            .HasDatabaseName("ix_players_normalized_user_name")
            .IsUnique();

        // Identity enforces RequireUniqueEmail in code only. The email is our
        // login identifier, so back it with a constraint the database owns.
        // Null emails stay permitted: Postgres treats nulls as distinct.
        builder.HasIndex(p => p.NormalizedEmail)
            .HasDatabaseName("ix_players_normalized_email")
            .IsUnique();
    }
}
