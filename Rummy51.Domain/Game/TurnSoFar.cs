namespace Rummy51.Domain.Game;

/// <summary>
/// Something a turn put on the table, a meld by its place there - which
/// holds still for a turn; see <see cref="Board.TakeBackTheLast"/>.
/// </summary>
internal abstract record Move
{
    public sealed record Laid(int Meld) : Move;

    public sealed record Joined(int Meld, HeldCard Card) : Move;

    /// <summary>Both cards: the one that paid, and the joker that came away.</summary>
    public sealed record Swapped(int Meld, int At, HeldCard Card, HeldCard Joker) : Move;
}

/// <summary>
/// What a turn has done and can still take back. It keeps the account;
/// putting the cards back belongs to the hand, which alone knows where they
/// came from. A card off the stock is here as drawn but never as one to take
/// back: putting it back would let a player look at the top of the stock and
/// then decide not to have looked.
/// </summary>
internal sealed class TurnSoFar
{
    private readonly List<Move> putDown = [];

    /// <summary>
    /// By number and not by position: a meld taken back puts its cards at the
    /// end of the hand, so the card drawn is not the last one for long.
    /// </summary>
    private (int Card, DrawnFrom From)? drawn;

    public bool Anything =>
        putDown.Count > 0 || TheDiscard is not null || TheFinishingCard is not null;

    public int? TheDiscard => From(DrawnFrom.Discards);

    public int? OffTheStock => From(DrawnFrom.Stock);

    /// <summary>Rule 7.4.</summary>
    public int? TheFinishingCard => From(DrawnFrom.Finishing);

    public void Took(int card, DrawnFrom from) => drawn = (card, from);

    public void GaveItBack() => drawn = null;

    /// <summary>
    /// Rule 3.4, the stock not being the table. What was taken, not what is
    /// still held: whether a card is back is read off the hand.
    /// </summary>
    public IEnumerable<int> Owed
    {
        get
        {
            foreach (var swap in putDown.OfType<Move.Swapped>())
            {
                yield return swap.Joker.Id;
            }

            if (TheDiscard is int card)
            {
                yield return card;
            }
        }
    }

    public void Laid(int meld) => putDown.Add(new Move.Laid(meld));

    /// <summary>Laid and not taken back, by their places. Rule 5.6.</summary>
    public IReadOnlyList<int> Laying => [.. putDown.OfType<Move.Laid>().Select(laid => laid.Meld)];

    public void Joined(int meld, HeldCard card) => putDown.Add(new Move.Joined(meld, card));

    public void Swapped(int meld, int at, HeldCard card, HeldCard joker) =>
        putDown.Add(new Move.Swapped(meld, at, card, joker));

    public Move? TakeBackTheLast()
    {
        if (putDown.Count == 0)
        {
            return null;
        }

        var last = putDown[^1];

        putDown.RemoveAt(putDown.Count - 1);

        return last;
    }

    public void Forget()
    {
        putDown.Clear();
        drawn = null;
    }

    private int? From(DrawnFrom pile) => drawn?.From == pile ? drawn.Value.Card : null;
}
