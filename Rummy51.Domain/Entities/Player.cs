using Microsoft.AspNetCore.Identity;

namespace Rummy51.Domain.Entities;

/// <summary>
/// A registered player. Authentication state (credentials, lockout, tokens)
/// is inherited from Identity; everything declared here belongs to the game.
/// </summary>
public class Player : IdentityUser<Guid>
{
    /// <summary>
    /// The name shown to other players. Unlike <see cref="IdentityUser{TKey}.UserName"/>,
    /// which doubles as the login identifier, this is free text and need not be unique.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Supplied by whoever signed the player in, when they offer one. Null for
    /// an account created with a password, until pictures can be chosen.
    /// </summary>
    public string? AvatarUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the player signs in for the first time.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
