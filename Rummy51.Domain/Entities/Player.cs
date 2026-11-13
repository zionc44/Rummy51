using Microsoft.AspNetCore.Identity;

namespace Rummy51.Domain.Entities;

/// <summary>
/// A registered player. Authentication state (credentials, lockout, tokens)
/// is inherited from Identity; everything declared here belongs to the game.
/// </summary>
public class Player : IdentityUser<Guid>
{
    /// <summary>Free text, unlike <see cref="IdentityUser{TKey}.UserName"/>.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Supplied by whoever signed the player in, when they offer one.</summary>
    public string? AvatarUrl { get; set; }

    public AvatarSource AvatarSource { get; set; }

    /// <summary>
    /// Rides in the picture's address, so the address changes whenever the
    /// picture does and the answer can be cached for good.
    /// </summary>
    public string? AvatarImageTag { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the player signs in for the first time.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
