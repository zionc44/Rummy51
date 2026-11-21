namespace Rummy51.Domain.Game;

/// <summary>
/// Rule 3.5: the stock runs out and what has been thrown becomes the next
/// one. It is its own file rather than part of the turn because it is not
/// a move - nobody chooses it and nobody may refuse it - and because the
/// one thing it asks of the table is the one thing a turn never asks:
/// that everybody has seen what happened before anything else does.
/// </summary>
public sealed partial class Hand
{
    /// <summary>
    /// Whether rule 3.5 has anything to do. There is no filling a stock
    /// from a pile holding only the card that has to stay on it.
    /// </summary>
    private bool TheStockHasRunOut => Board.Stock.Count == 0 && Board.Discards.Count > 1;

    /// <summary>
    /// Rule 3.5, seen. Nobody is being asked anything - there is no hand
    /// without a stock - so this only records who the table need not wait
    /// for any longer.
    /// </summary>
    public bool SeeTheRestock(int seat)
    {
        if (Phase != HandPhase.Restocking || seat < 0 || seat >= Seats || !playing[seat])
        {
            return false;
        }

        seenTheRestock[seat] = true;

        if (Enumerable.Range(0, Seats).All(one => !playing[one] || seenTheRestock[one]))
        {
            Restock();
        }

        return true;
    }

    /// <summary>Long enough to have seen it, whether anybody did or not.</summary>
    public bool RestockForWhoeverHasNot()
    {
        if (Phase != HandPhase.Restocking)
        {
            return false;
        }

        Restock();

        return true;
    }

    /// <summary>
    /// Rule 3.5. The order it came out in is written down because nothing
    /// else could reach it again: what goes into a restock is whatever the
    /// table happened to throw, which no seed describes.
    /// </summary>
    private void Restock()
    {
        var thrown = Board.TakeTheDiscardsUnderTheTop();
        var shuffled = Deck.Shuffled(thrown, shuffling);

        Board.StockWith(shuffled);
        restocks.Add(string.Join(',', shuffled.Select(card => card.Code)));

        Array.Clear(seenTheRestock);
        Phase = HandPhase.Playing;
    }
}
