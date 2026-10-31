using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence;

public class Rummy51DbContext(DbContextOptions<Rummy51DbContext> options)
    : IdentityDbContext<Player, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity builds its own model first; everything below refines it.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(Rummy51DbContext).Assembly);

        // The remaining Identity tables have no entity class of our own, so
        // they are renamed here rather than in a configuration file. Column
        // names are handled by the snake_case convention.
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("player_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("player_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("player_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("player_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
