namespace Rummy51.Infrastructure.Identity;

/// <summary>
/// Bound from the "Authentication" section of configuration, so the timings
/// can be changed without touching code.
/// </summary>
public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// How long a session survives without a request. Every request restarts
    /// the clock, so this is an idle timeout rather than a session length.
    /// </summary>
    public int IdleTimeoutMinutes { get; init; } = 30;

    /// <summary>How long a "remember me" sign-in lasts across browser restarts.</summary>
    public int RememberMeDays { get; init; } = 30;
}
