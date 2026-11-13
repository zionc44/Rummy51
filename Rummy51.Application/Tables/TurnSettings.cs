namespace Rummy51.Application.Tables;

/// <summary>
/// How long the table waits before rule 3.6 acts for a player - its own
/// business, which is why this is configuration. Three numbers rather than
/// one with arithmetic, half of fifteen being seven and a half.
///
/// The clock is not there to hurry anybody. It is there so that one slow
/// player cannot hold three others still.
/// </summary>
public sealed class TurnSettings
{
    public const string SectionName = "Turns";

    /// <summary>One click on a deck nobody can read, so it needs less than a turn.</summary>
    public int CutSeconds { get; init; } = 8;

    /// <summary>
    /// The server dealt them the moment the deck was cut, but on the screen
    /// they are still going out, and the first player's time should not be
    /// spent watching that.
    /// </summary>
    public int DealSeconds { get; init; } = 5;

    /// <summary>
    /// The first player has a whole hand to take in, sort and decide about
    /// before they discard, which is more than any later turn asks.
    /// </summary>
    public int FirstTurnSeconds { get; init; } = 30;

    public int TurnSeconds { get; init; } = 15;
}
