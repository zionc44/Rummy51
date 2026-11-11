namespace Rummy51.Domain.Game;

/// <summary>
/// Something a turn put on the table. A meld needs nothing said about it:
/// melds are only ever laid at the end of the table and only ever taken back
/// in the turn that laid them, so the last one laid is the last one lying
/// there. A card that joined a meld has to say which one.
/// </summary>
internal abstract record Move
{
    /// <summary>
    /// A meld laid, by its place on the table. Melds are only ever put at
    /// the end and only ever taken back in the turn that laid them, so the
    /// places this turn used stay where they are until it is over.
    /// </summary>
    public sealed record Laid(int Meld) : Move;

    public sealed record Joined(int Meld, HeldCard Card) : Move;

    /// <summary>
    /// A joker bought out of a meld. Both cards are kept: the one that went
    /// onto the table to be taken off again, and the joker that came away
    /// to be put back where it was lying.
    /// </summary>
    public sealed record Swapped(int Meld, int At, HeldCard Card, HeldCard Joker) : Move;
}

/// <summary>
/// What a turn has done and can still take back - and nothing else. It
/// keeps the account; putting the cards back where they came from belongs
/// to the hand, which is the only thing that knows where that is.
///
/// A card off the stock is in here as a card drawn but never as something
/// to take back. Putting it where it came from would let a player look at
/// the top of the stock and then decide not to have looked, and there is no
/// undoing knowing something.
/// </summary>
internal sealed class TurnSoFar
{
    private readonly List<Move> putDown = [];

    /// <summary>
    /// The card drawn this turn and where it came from, by number and not
    /// by position: a meld taken back puts its cards at the end of the
    /// hand, so the card that was drawn is not the last one for long.
    /// </summary>
    private (int Card, DrawnFrom From)? drawn;

    public bool Anything => putDown.Count > 0 || TheDiscard is not null;

    /// <summary>The card taken off the discard pile, which can go back.</summary>
    public int? TheDiscard => From(DrawnFrom.Discards);

    /// <summary>The card off the stock, which is the one a turn is spent on.</summary>
    public int? OffTheStock => From(DrawnFrom.Stock);

    public void Took(int card, DrawnFrom from) => drawn = (card, from);

    /// <summary>The draw undone, which only the discard pile ever allows.</summary>
    public void GaveItBack() => drawn = null;

    /// <summary>
    /// What this turn took off the table and owes back to it: the card off
    /// the discard pile, and every joker bought out of a meld. A card off
    /// the stock owes nothing, because the stock is not the table - it came
    /// from nowhere anybody could see and goes wherever its holder likes.
    ///
    /// It is an account of what was taken and not of what is still held.
    /// Whether a card is back on the table is read off the hand, which is
    /// the only thing that knows what is in it.
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

    /// <summary>
    /// The melds this turn has laid and not taken back, by their places on
    /// the table. What they come to between them is what the fifty-one is
    /// asked of.
    /// </summary>
    public IReadOnlyList<int> Laying => [.. putDown.OfType<Move.Laid>().Select(laid => laid.Meld)];

    public void Joined(int meld, HeldCard card) => putDown.Add(new Move.Joined(meld, card));

    public void Swapped(int meld, int at, HeldCard card, HeldCard joker) =>
        putDown.Add(new Move.Swapped(meld, at, card, joker));

    /// <summary>
    /// The last thing put on the table, taken off the account. Null when
    /// this turn has put nothing there.
    /// </summary>
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

    /// <summary>Nothing of one turn is left to take back in the next.</summary>
    public void Forget()
    {
        putDown.Clear();
        drawn = null;
    }

    private int? From(DrawnFrom pile) => drawn?.From == pile ? drawn.Value.Card : null;
}
