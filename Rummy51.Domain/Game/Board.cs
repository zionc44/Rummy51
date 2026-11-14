namespace Rummy51.Domain.Game;

/// <summary>
/// What is lying on the felt. It knows where the top of each pile is and
/// nothing else: who may reach for what, and whether what is being laid is a
/// run at all, are rules and belong to <see cref="Hand"/>. This is the table
/// itself, which holds whatever is put on it.
/// </summary>
public sealed class Board
{
    private readonly List<Card> stock = [];
    private readonly List<Card> discards = [];
    private readonly List<Meld> melds = [];

    public IReadOnlyList<Card> Stock => stock;

    /// <summary>Oldest first. Rule 3.3.</summary>
    public IReadOnlyList<Card> Discards => discards;

    public Card? TopOfDiscards => discards.Count == 0 ? null : discards[^1];

    /// <summary>In the order it was laid there.</summary>
    public IReadOnlyList<Meld> Melds => melds;

    public void StockWith(IEnumerable<Card> cards) => stock.AddRange(cards);

    public Card? TakeFromStock()
    {
        if (stock.Count == 0)
        {
            return null;
        }

        var card = stock[0];

        stock.RemoveAt(0);

        return card;
    }

    /// <summary>Rule 3.3.</summary>
    public Card? TakeFromDiscards()
    {
        if (discards.Count == 0)
        {
            return null;
        }

        var card = discards[^1];

        discards.RemoveAt(discards.Count - 1);

        return card;
    }

    public void Throw(Card card) => discards.Add(card);

    public void Lay(Meld meld) => melds.Add(meld);

    public void Grow(int at, MeldEnd end, HeldCard card)
    {
        var meld = melds[at];

        melds[at] = meld with
        {
            Cards = end == MeldEnd.Front ? [card, .. meld.Cards] : [.. meld.Cards, card],
        };
    }

    /// <summary>Rule 5.11, and how the joker goes back if it is taken back.</summary>
    public HeldCard Swap(int at, int place, HeldCard card)
    {
        var meld = melds[at];
        var was = meld.Cards[place];

        melds[at] = meld with
        {
            Cards = [.. meld.Cards.Select((lying, where) => where == place ? card : lying)],
        };

        return was;
    }

    public void Shrink(int at, int cardId)
    {
        var meld = melds[at];

        melds[at] = meld with { Cards = [.. meld.Cards.Where(card => card.Id != cardId)] };
    }

    /// <summary>Rule 12.5: everything picked up, and the table bare.</summary>
    public IReadOnlyList<Meld> ClearTheTable()
    {
        var lying = melds.ToList();

        melds.Clear();

        return lying;
    }

    /// <summary>
    /// Melds are only ever put at the end of the table and only ever taken
    /// back in the turn that laid them, so the last one laid is the last one
    /// lying here and this needs no index.
    /// </summary>
    public Meld? TakeBackTheLast()
    {
        if (melds.Count == 0)
        {
            return null;
        }

        var meld = melds[^1];

        melds.RemoveAt(melds.Count - 1);

        return meld;
    }
}
