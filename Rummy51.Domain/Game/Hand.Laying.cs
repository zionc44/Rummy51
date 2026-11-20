namespace Rummy51.Domain.Game;

/// <summary>
/// What goes from a hand onto the table. Laying a meld of one's own,
/// adding a card to one already lying there, and buying a joker out of one
/// - three moves that differ in what they cost and agree in what they do.
/// </summary>
public sealed partial class Hand
{
    /// <summary>
    /// Rules 4.1 and 6.2. What it comes to is not asked here: rule 5.6 asks
    /// that of a whole turn, when the turn ends.
    /// </summary>
    public bool LayDown(int seat, IReadOnlyList<int> cardIds)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || cardIds.Count == 0)
        {
            return false;
        }

        // One card named twice would be laid twice and taken out once.
        if (cardIds.Distinct().Count() != cardIds.Count)
        {
            return false;
        }

        var held = hands[seat];
        var places = cardIds.Select(id => held.FindIndex(card => card.Id == id)).ToList();

        if (places.Contains(-1))
        {
            return false;
        }

        if (!CanPartWith(seat, places.Count))
        {
            return false;
        }

        var meld = new Meld(seat, [.. places.Select(at => Owned(held[at], seat))]);

        if (!Melding.IsMeld([.. meld.Cards.Select(lying => lying.Card)]))
        {
            return false;
        }

        Board.Lay(meld);
        turn.Laid(Board.Melds.Count - 1);

        // From the back, so that taking one out does not move the places of
        // those still to be taken.
        foreach (var at in places.OrderDescending())
        {
            held.RemoveAt(at);
        }

        return true;
    }

    /// <summary>Rules 5.7, 5.9, 5.10, 6.2.</summary>
    public bool AddTo(int seat, int meldAt, int cardId, MeldEnd end)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || !CanAddToTheTable(seat))
        {
            return false;
        }

        if (meldAt < 0 || meldAt >= Board.Melds.Count)
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1 || !CanPartWith(seat, 1))
        {
            return false;
        }

        var card = hands[seat][held];
        var meld = Board.Melds[meldAt];

        if (!Melding.WhereItFits([.. meld.Cards.Select(lying => lying.Card)], card.Card).Contains(end))
        {
            return false;
        }

        Board.Grow(meldAt, end, Owned(card, seat));
        hands[seat].RemoveAt(held);
        turn.Joined(meldAt, card);

        return true;
    }

    /// <summary>
    /// Rules 5.11, 5.12, 6.3. The joker comes into the hand keeping the
    /// number it has had since it was dealt, which is what lets the screen
    /// follow one across the table.
    /// </summary>
    public bool SwapForJoker(int seat, int meldAt, int at, int cardId)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || !CanAddToTheTable(seat))
        {
            return false;
        }

        if (meldAt < 0 || meldAt >= Board.Melds.Count)
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1)
        {
            return false;
        }

        var card = hands[seat][held];
        var meld = Board.Melds[meldAt];

        if (!Melding.CanReplace([.. meld.Cards.Select(lying => lying.Card)], at, card.Card))
        {
            return false;
        }

        var joker = Board.Swap(meldAt, at, Owned(card, seat));

        hands[seat].RemoveAt(held);
        hands[seat].Add(joker);
        turn.Swapped(meldAt, at, card, joker);

        return true;
    }
}
