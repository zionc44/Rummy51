namespace Rummy51.Application.Tables;

/// <summary>
/// How long the table waits for a player before it acts for them. Seconds,
/// and three of them rather than one with arithmetic: half of fifteen is
/// seven and a half, and half a second is not a thing worth explaining.
///
/// The point of the clock is not to hurry anybody. It is that one slow
/// player should not be able to hold three others still.
/// </summary>
public sealed class TurnSettings
{
    public const string SectionName = "Turns";

    /// <summary>
    /// Choosing where to cut is one click on a deck nobody can read, so it
    /// needs less time than a turn and gets it.
    /// </summary>
    public int CutSeconds { get; init; } = 8;

    /// <summary>
    /// How long the cards take to reach the seats. The server dealt them
    /// the moment the deck was cut, but on the screen they are still going
    /// out, and the first player's time should not be spent watching that.
    /// </summary>
    public int DealSeconds { get; init; } = 5;

    /// <summary>
    /// The first player has a whole hand to take in, sort and decide about
    /// before they discard, which is more than any later turn asks.
    /// </summary>
    public int FirstTurnSeconds { get; init; } = 30;

    public int TurnSeconds { get; init; } = 15;
}
