namespace Rummy51.Domain.Game;

/// <summary>
/// Rules 12.9 to 12.13: a redeal nobody gets to refuse outright. It runs
/// through the same offer as 12.1 and settles in the same place, and what
/// is its own is who may force one and what they are holding.
/// </summary>
public sealed partial class Hand
{
    /// <summary>Rule 12.9.</summary>
    private const int Pairs = 4;

    private readonly bool[] hasPlayed;

    /// <summary>
    /// Rule 12.9. A joker is neither a rank nor a suit, so two of them are
    /// not the same card twice however alike they look.
    /// </summary>
    public IReadOnlyList<string> FourPairsOf(int seat) =>
    [
        .. hands[seat]
            .Where(card => card.Card is Card.Suited)
            .GroupBy(card => card.Card.Code)
            .Where(same => same.Count() > 1)
            .Take(Pairs)
            .SelectMany(same => same.Select(card => card.Card.Code))
    ];

    /// <summary>Rule 12.9: this offer cannot simply be refused.</summary>
    public bool Forced => offer?.Forced == true;

    /// <summary>
    /// Rule 12.11: the pairs it was forced with, which the table reads once
    /// the deal is actually going. While the question is still open they are
    /// the forcer's own cards and rule 11.1 covers them like any others - a
    /// threat is worth no less for being unseen.
    /// </summary>
    public IReadOnlyList<string> ForcedWith =>
        Forced && Phase == HandPhase.Answered ? FourPairsOf(offer!.By) : [];

    /// <summary>Rule 12.9, which does not wait for a turn and does not last past one.</summary>
    public bool CanForceARedeal(int seat) =>
        Phase == HandPhase.Playing
        && seat >= 0
        && seat < Seats
        && !hasPlayed[seat]
        && FourPairsOf(seat).Count == Pairs * 2;

    /// <summary>Rules 12.10 and 12.13.</summary>
    public bool ForceARedeal(int seat, bool atOnce)
    {
        if (!CanForceARedeal(seat))
        {
            return false;
        }

        Undo(TurnSeat);

        offer = new Offer(seat, Seats, forced: true, atOnce);
        Phase = atOnce ? HandPhase.Answered : HandPhase.Offering;

        return true;
    }
}
