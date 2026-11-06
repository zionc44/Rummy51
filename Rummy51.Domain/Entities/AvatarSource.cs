namespace Rummy51.Domain.Entities;

/// <summary>
/// Which picture a player wants shown. Both can exist at once: the provider
/// keeps refreshing its own on every sign-in, and choosing the other one has
/// to survive that rather than be overwritten by it.
/// </summary>
public enum AvatarSource
{
    /// <summary>Whoever signed the player in. Null for an account with only a password.</summary>
    Provider,

    /// <summary>The picture the player uploaded and cropped.</summary>
    Uploaded
}
