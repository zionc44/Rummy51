namespace Rummy51.Domain.Game;

/// <summary>Rules 8.1 to 8.6: what a hand came to for each seat, and why.</summary>
public sealed partial class Hand
{
    /// <summary>Rule 8.1, and what the screen reads while the hand is still running.</summary>
    public int WorthOf(int seat) =>
        IsDown(seat) ? Worth.Held(hands[seat].Select(card => card.Card)) : Worth.NeverWentDown;

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
    public IReadOnlyList<int> Places =>
        Scores.Count == 0 ? [] : Placing.Of(Scores, playing);

    /// <summary>Rules 8.4 and 8.5, told by the game: which hand of a series this is is not the cards'.</summary>
    public HandWeight Weight { get; init; } = HandWeight.Single;

    /// <summary>Rules 8.2 and 8.6, and one until there is something to multiply.</summary>
    public int ResultMultiplier => FixedMultiplier * RedealMultiplier * WinMultiplier;

    /// <summary>Rule 8.4.</summary>
    public int FixedMultiplier => Phase == HandPhase.Finished ? Weight.Fixed : 1;

    /// <summary>Rules 8.2 and 8.5.</summary>
    public int RedealMultiplier =>
        Phase == HandPhase.Finished
        && (Weight.Redealt || (Weight.CountsSingle && AgreedRedeals > 0)) ? 2 : 1;

    /// <summary>Rule 8.2: taken by somebody who owed the table nothing for it.</summary>
    public int WinMultiplier =>
        Phase == HandPhase.Finished
        && WinnerSeat != -1
        && (wentRummy[WinnerSeat] || tookTheFinishingCard[WinnerSeat]) ? 2 : 1;

    // A seat that was not in the hand pays nothing for it. Rule 8.1 is
    // about cards somebody is holding, and they were dealt none.
    private int Scored(int seat) =>
        !playing[seat] ? 0
        : (seat == WinnerSeat ? Worth.Winner : WorthOf(seat)) * ResultMultiplier;
}
