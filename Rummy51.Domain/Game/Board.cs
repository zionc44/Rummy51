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

    /// <summary>
    /// A meld picked up again, which only ever happens in the turn that laid
    /// it: once a turn has ended, what is on the table stays there.
    /// </summary>
    public void TakeBack(Meld meld) => melds.Remove(meld);
}
