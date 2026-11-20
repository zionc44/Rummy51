namespace Rummy51.Domain.Game;

/// <summary>
/// Rules 12.5 to 12.8: what a redeal that was taken actually costs. Three
/// cards each are chosen out of what everybody is holding, everything else
/// becomes a deck again, and it is dealt from the cut - so what comes out
/// is the same hand one round shorter and not a new one.
/// </summary>
public sealed partial class Hand
{
    private readonly int[]?[] keeping;
    private int nextDeal = Dealing.FirstDeal;

    /// <summary>How many of their own a player keeps through a redeal. Rule 12.6.</summary>
    public const int Kept = 3;

    /// <summary>Everybody this hand was dealt to, the rest having nothing to keep.</summary>
    private bool EverybodyHasKept =>
        Enumerable.Range(0, Seats).All(seat => !playing[seat] || keeping[seat] is not null);

    /// <summary>Rule 12.6: exactly three, and their own.</summary>
    public bool Keep(int seat, IReadOnlyList<int> cardIds)
    {
        if (Phase != HandPhase.Keeping
            || seat < 0
            || seat >= Seats
            || !playing[seat]
            || keeping[seat] is not null)
        {
            return false;
        }

        if (cardIds.Count != Kept || cardIds.Distinct().Count() != Kept)
        {
            return false;
        }

        if (cardIds.Any(id => hands[seat].All(card => card.Id != id)))
        {
            return false;
        }

        keeping[seat] = [.. cardIds];

        if (EverybodyHasKept)
        {
            DealAgain();
        }

        return true;
    }

    /// <summary>
    /// Rule 12.6 for whoever has not chosen. The dearest three, which puts
    /// the jokers first: twenty in the hand is the most any card is worth,
    /// so the one sort answers both.
    /// </summary>
    public bool KeepForWhoeverHasNot()
    {
        if (Phase != HandPhase.Keeping)
        {
            return false;
        }

        for (var seat = 0; seat < Seats; seat++)
        {
            if (!playing[seat])
            {
                keeping[seat] = [];

                continue;
            }

            keeping[seat] ??=
            [
                .. hands[seat]
                    .OrderByDescending(card => Worth.Held(card.Card))
                    .Take(Kept)
                    .Select(card => card.Id)
            ];
        }

        DealAgain();

        return true;
    }

    /// <summary>
    /// Rules 12.6, 12.7 and 12.8. Everything but the kept cards goes into a
    /// deck again and the hand starts at the cut, so the cutter cuts and the
    /// deal runs exactly as they did the first time - one round shorter.
    /// </summary>
    private void DealAgain()
    {
        var kept = keeping.Sum(three => three!.Length);
        var loose = new List<Card>(Board.Stock);

        loose.AddRange(Board.Discards);

        if (Deck.FinishingCard is { } finishing)
        {
            loose.Add(finishing);
        }

        for (var seat = 0; seat < Seats; seat++)
        {
            var keep = keeping[seat]!.ToHashSet();

            loose.AddRange(hands[seat].Where(card => !keep.Contains(card.Id)).Select(card => card.Card));
            hands[seat].RemoveAll(card => !keep.Contains(card.Id));
            keeping[seat] = null;
        }

        // Rule 12.7 deals twelve and eleven on top of three kept; rule
        // 12.12 keeps none, so the hand is dealt from nothing as it was.
        nextDeal = kept == 0 ? Dealing.FirstDeal : Dealing.Redeal;

        Deck = new Deck(loose, Deck.Seed);
        Board = new Board();
        Passes = [];

        Array.Fill(wentDown, false);
        Array.Fill(wentRummy, false);
        Array.Fill(tookTheFinishingCard, false);
        Array.Fill(hasOffered, false);
        Array.Fill(hasPlayed, false);

        turnsTaken = 0;
        TurnSeat = -1;
        HasDrawn = false;
        Phase = HandPhase.Cutting;

        Forget();
    }

    /// <summary>Rule 12.12: nobody keeps anything, so nobody is asked.</summary>
    private void DealAgainKeepingNothing()
    {
        Array.Fill(keeping, []);
        DealAgain();
    }
}
