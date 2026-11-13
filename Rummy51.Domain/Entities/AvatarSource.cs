namespace Rummy51.Domain.Entities;

/// <summary>
/// Which picture a player wants shown. Both can exist at once: the provider
/// keeps refreshing its own on every sign-in, and choosing the other one has
/// to survive that rather than be overwritten by it.
/// </summary>
public enum AvatarSource
{
    Provider,

    Uploaded
}
