namespace Rummy51.Domain.Game;

/// <summary>
/// Rules 8.1, 8.2 and 8.3: what a hand came to for each seat, and why.
/// It answers for every seat or for none, because one seat takes a minus
/// and the rest pay: no seat's number means anything without the one that
/// won, and until a hand is over there is no winner to take it.
/// </summary>
public sealed partial class Hand
{
    /// <summary>Rule 8.1, and what the screen reads while the hand is still running.</summary>
    public int WorthOf(int seat) =>
        wentDown[seat] ? Worth.Held(hands[seat].Select(card => card.Card)) : Worth.NeverWentDown;

    /// <summary>
    /// Rule 8.1 for the whole table at once, because that is the shape it
    /// has: one seat takes a minus and the rest pay, and no seat's number
    /// means anything without the one that won. Empty until a hand is over,
    /// since until then there is no winner to take the minus.
    /// </summary>
    public IReadOnlyList<int> Scores =>
        Phase == HandPhase.Finished
            ? [.. Enumerable.Range(0, Seats).Select(Scored)]
            : [];

    /// <summary>Rule 8.3, by seat, and empty alongside empty scores.</summary>
    public IReadOnlyList<int> Places => Placing.Of(Scores);

    /// <summary>
    /// Rule 8.2, which is the table's and not any player's: it multiplies
    /// the winner's minus and everybody's penalty alike. One while a hand
    /// is still being played, there being nothing yet to multiply.
    /// </summary>
    public int ResultMultiplier => RedealMultiplier * WinMultiplier;

    /// <summary>Rule 8.2: a redeal the table agreed to, however many there were.</summary>
    public int RedealMultiplier =>
        Phase == HandPhase.Finished && AgreedRedeals > 0 ? 2 : 1;

    /// <summary>Rule 8.2: taken by somebody who owed the table nothing for it.</summary>
    public int WinMultiplier =>
        Phase == HandPhase.Finished
        && WinnerSeat != -1
        && (wentRummy[WinnerSeat] || tookTheFinishingCard[WinnerSeat]) ? 2 : 1;

    private int Scored(int seat) =>
        (seat == WinnerSeat ? Worth.Winner : WorthOf(seat)) * ResultMultiplier;
}
