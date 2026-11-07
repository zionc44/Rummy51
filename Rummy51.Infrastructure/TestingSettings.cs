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
    /// Tells every client where the jokers are lying in the deck, so a cut
    /// onto one can be aimed at. It is plainly a cheat - which is why it
    /// cannot be switched on in a real game - and the reason it exists is
    /// that a joker at the cut is far too rare to meet by playing.
    /// </summary>
    public bool MarkJokers { get; init; }
}
