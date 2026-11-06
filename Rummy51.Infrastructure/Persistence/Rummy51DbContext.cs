using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Persistence;

public class Rummy51DbContext(DbContextOptions<Rummy51DbContext> options)
    : IdentityDbContext<Player, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>
    /// The keys that encrypt the authentication cookie. They belong in the
    /// database rather than on disk: a host that is replaced or scaled out
    /// would otherwise lose them and sign every player out.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<PlayerAvatar> PlayerAvatars => Set<PlayerAvatar>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity builds its own model first; everything below refines it.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(Rummy51DbContext).Assembly);

        // The remaining Identity tables have no entity class of our own, so
        // they are renamed here rather than in a configuration file. Column
        // names are handled by the snake_case convention.
        builder.Entity<IdentityRole<Guid>>(role =>
        {
            role.ToTable("roles");
            role.HasIndex(r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name");
        });
        builder.Entity<IdentityUserRole<Guid>>().ToTable("player_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("player_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("player_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("player_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<DataProtectionKey>().ToTable("data_protection_keys");
    }
}
