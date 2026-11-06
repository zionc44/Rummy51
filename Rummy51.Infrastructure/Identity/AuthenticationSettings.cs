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

    public GoogleSettings Google { get; init; } = new();
}

/// <summary>
/// From the Google Cloud console. Both are supplied through user-secrets in
/// development, never through a file that git can see: unlike the database
/// password these authorise an app against a service on the internet.
/// </summary>
public sealed class GoogleSettings
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured => ClientId.Length > 0 && ClientSecret.Length > 0;
}
