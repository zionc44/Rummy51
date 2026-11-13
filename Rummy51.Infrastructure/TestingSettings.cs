namespace Rummy51.Infrastructure;

/// <summary>
/// Aids for testing a game by hand. They are bound only when the host is
/// running in development, so a section of these in a deployed configuration
/// is read by nobody, and every one of them is off until it is asked for.
/// </summary>
public sealed class TestingSettings
{
    public const string SectionName = "Testing";

    /// <summary>
    /// Breaks rule 11.5 on purpose, a joker at the cut being far too rare to
    /// meet by playing. Plainly a cheat, which is why it is here.
    /// </summary>
    public bool MarkJokers { get; init; }
}
