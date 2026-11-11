namespace Rummy51.Domain.Game;

/// <summary>
/// What is lying on the felt: the stock still to be drawn from, the pile
/// thrown onto, and the runs and sets laid down.
///
/// It knows where the top of each pile is and nothing else. Who may reach
/// for what, and when, and whether what is being laid is a run at all, are
/// rules of the game and belong to <see cref="Hand"/>. This is the table
/// itself, which holds whatever is put on it.
/// </summary>
public sealed class Board
{
    private readonly List<Card> stock = [];
    private readonly List<Card> discards = [];
    private readonly List<Meld> melds = [];

    /// <summary>What is left to draw from once the deal is done.</summary>
    public IReadOnlyList<Card> Stock => stock;

    /// <summary>
    /// What has been thrown away, oldest first. Only the last of them is any
    /// player's business, and that is all the table is ever told.
    /// </summary>
    public IReadOnlyList<Card> Discards => discards;

    /// <summary>The one card of the pile lying face up, or null before any.</summary>
    public Card? TopOfDiscards => discards.Count == 0 ? null : discards[^1];

    /// <summary>
    /// What is lying on the table, in the order it was laid there. Nobody
    /// owns it any more in the sense of holding it, but whose it was still
    /// matters, so each one says.
    /// </summary>
    public IReadOnlyList<Meld> Melds => melds;

    /// <summary>Whatever the deal did not hand out, which becomes the stock.</summary>
    public void StockWith(IEnumerable<Card> cards) => stock.AddRange(cards);

    /// <summary>
    /// Off the front of the stock, which is where the deal left off. Null
    /// once there is nothing left there to take.
    /// </summary>
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

    /// <summary>
    /// Off the back of the discard pile, which is where the player before
    /// threw, and the only card of that pile anybody may take. Null while
    /// nothing has been thrown at all.
    /// </summary>
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

    /// <summary>A card thrown away: face up, on top of the rest.</summary>
    public void Throw(Card card) => discards.Add(card);

    public void Lay(Meld meld) => melds.Add(meld);

    /// <summary>A card joining a meld already lying here, at one end of it.</summary>
    public void Grow(int at, MeldEnd end, HeldCard card)
    {
        var meld = melds[at];

        melds[at] = meld with
        {
            Cards = end == MeldEnd.Front ? [card, .. meld.Cards] : [.. meld.Cards, card],
        };
    }

    /// <summary>
    /// A card put where a card of that meld is lying, and the one that was
    /// there handed back. It is how a joker is bought out of a meld, and how
    /// it is put back again when the buyer changes their mind.
    /// </summary>
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

    /// <summary>And that card taken off it again.</summary>
    public void Shrink(int at, int cardId)
    {
        var meld = melds[at];

        melds[at] = meld with { Cards = [.. meld.Cards.Where(card => card.Id != cardId)] };
    }

    /// <summary>
    /// The last meld laid, picked up again. Melds are only ever put at the
    /// end of the table and only ever taken back in the turn that laid them,
    /// so the last one laid is the last one lying here. Null where the table
    /// is bare.
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
