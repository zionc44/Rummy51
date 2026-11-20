namespace Rummy51.Domain.Game;

/// <summary>
/// What a hand answers without being changed by the asking. Every one of
/// these is a question the screen puts before a move and the move puts
/// again before making it, which is why they are gathered rather than
/// each kept beside the move it guards.
/// </summary>
public sealed partial class Hand
{
    /// <summary>Rules 3.4, 5.6, 7.2. Laying nothing is always allowed.</summary>
    public bool CanEndTheTurn(int seat) =>
        HasPutBackWhatItTook(seat)
        && !MustStillGoOut(seat)
        && (wentDown[seat] || turn.Laying.Count == 0 || Melding.IsAShowing(Showing));

    /// <summary>Rule 6.4, counted in the seats that are in this hand.</summary>
    public bool CanFinish => turnsTaken >= Players - 1;

    /// <summary>Rules 7.1, 7.3, and 6.4 because taking it is going out.</summary>
    public bool CanTakeTheFinishingCard(int seat) =>
        IsTheirTurn(seat)
        && !HasDrawn
        && !wentDown[seat]
        && CanFinish
        && Deck.FinishingCard is not null;

    /// <summary>Rule 7.1: taking it promised the table a win this turn.</summary>
    private bool PromisedToGoOut => turn.TheFinishingCard is not null;

    /// <summary>Rule 7.4: a turn that cannot keep the promise ends no other way.</summary>
    public bool MustStillGoOut(int seat) => PromisedToGoOut && hands[seat].Count != 1;

    /// <summary>
    /// Rules 6.2 and 6.5. Two rather than one is how 6.4 is held: a hand that
    /// never reaches its last card can never be made to throw it, so nobody
    /// is stranded holding a card they are forbidden to throw.
    /// </summary>
    private int KeepsBack => CanFinish ? 1 : 2;

    /// <summary>Rule 6.2, which is what makes going out a throw and not a lay.</summary>
    public bool CanPartWith(int seat, int many) => hands[seat].Count - many >= KeepsBack;

    /// <summary>
    /// Rule 3.4. The card about to be thrown is still in the hand when this
    /// is asked, which is what stops a player throwing back the very card
    /// they took.
    /// </summary>
    public bool HasPutBackWhatItTook(int seat) => StillOwed(seat).Count == 0;

    /// <summary>
    /// So the player can be shown the card rather than told one exists.
    /// Empty for a seat not playing: a turn's debts are its own.
    /// </summary>
    public IReadOnlyList<int> StillOwed(int seat) =>
        IsTheirTurn(seat)
            ? [.. turn.Owed.Where(owed => hands[seat].Any(card => card.Id == owed))]
            : [];

    /// <summary>Rules 5.7, 5.8, 5.12, and 7.2 while the finishing card is held.</summary>
    public bool CanAddToTheTable(int seat) =>
        !PromisedToGoOut
        && (wentDown[seat] || (turn.Laying.Count > 0 && Melding.IsAShowing(Showing)));

    /// <summary>Rule 5.6: one turn is one move.</summary>
    private IReadOnlyList<IReadOnlyList<Card>> Showing => [.. turn.Laying.Select(Lying)];
}
