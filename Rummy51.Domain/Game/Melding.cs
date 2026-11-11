namespace Rummy51.Domain.Game;

/// <summary>Which end of a meld a card joins it at.</summary>
public enum MeldEnd
{
    Front,
    Back
}

/// <summary>
/// What makes a run or a set, and where a card can join one that is already
/// lying on the table.
///
/// Where a card fits is asked rather than worked out: the card is put at one
/// end and then the other, and whatever is still a meld afterwards is a
/// place it fits. That way the ace, the jokers and the four suits of a set
/// are each answered once, here, and not again for every question about
/// them.
/// </summary>
public static class Melding
{
    /// <summary>The fewest cards that can be a run or a set.</summary>
    private const int Least = 3;

    /// <summary>The most that can be a set, there being four suits.</summary>
    private const int Most = 4;

    /// <summary>
    /// The ends of this meld that card could join it at: both, one, or
    /// none at all.
    /// </summary>
    public static IReadOnlyList<MeldEnd> WhereItFits(IReadOnlyList<Card> meld, Card card)
    {
        if (meld.Count < Least)
        {
            return [];
        }

        // A set has no ends. Four of a rank are the same four whichever way
        // round they lie, so there is one place to put a card in one and
        // offering two would be offering the same place twice.
        if (IsSet(meld))
        {
            return IsSet([.. meld, card]) ? [MeldEnd.Back] : [];
        }

        List<MeldEnd> ends = [];

        if (IsRun([card, .. meld]))
        {
            ends.Add(MeldEnd.Front);
        }

        if (IsRun([.. meld, card]))
        {
            ends.Add(MeldEnd.Back);
        }

        return ends;
    }

    /// <summary>
    /// Whether that card is the one the joker lying there is standing in
    /// for. Asked the same way: the card is put in the joker's place, and
    /// if what is left is still a meld then that is the card the place was
    /// asking for all along. In a run the place names one card exactly; in
    /// a set it names the rank and leaves the suit to whichever the set has
    /// not got.
    /// </summary>
    public static bool CanReplace(IReadOnlyList<Card> meld, int at, Card card)
    {
        // Only a joker is ever bought out, and only with a card that names
        // itself: a joker for a joker is not a card coming off the table.
        if (at < 0 || at >= meld.Count || meld[at] is not Card.Joker || card is Card.Joker)
        {
            return false;
        }

        // A set gives up its joker only once it is whole. Two sevens and a
        // joker are two cards and a stand-in, and taking the stand-in away
        // leaves three sevens that were never four - nothing was completed
        // and nothing was paid. So the missing suit joins the set first,
        // and the one after it buys the joker.
        if (meld.Count < Most && IsSet(meld))
        {
            return false;
        }

        var swapped = meld.ToArray();

        swapped[at] = card;

        return IsMeld(swapped);
    }

    public static bool IsMeld(IReadOnlyList<Card> cards) =>
        cards.Count >= Least && (IsSet(cards) || IsRun(cards));

    /// <summary>Three or four of one rank, each from a different suit.</summary>
    private static bool IsSet(IReadOnlyList<Card> cards)
    {
        if (cards.Count > Most)
        {
            return false;
        }

        var named = cards.OfType<Card.Suited>().ToList();

        if (named.Count == 0)
        {
            return false;
        }

        var rank = named[0].Rank;
        var suits = named.Select(card => card.Suit).ToHashSet();

        return named.All(card => card.Rank == rank) && suits.Count == named.Count;
    }

    /// <summary>
    /// Three or more of one suit in order, up or down as they happen to be
    /// laid, with a joker standing in for whatever is missing. Q-K-A is a
    /// run and so is A-2-3, which is the whole reason the ace is asked
    /// about twice.
    /// </summary>
    private static bool IsRun(IReadOnlyList<Card> cards)
    {
        var named = cards.OfType<Card.Suited>().ToList();

        if (named.Count == 0 || named.Any(card => card.Suit != named[0].Suit))
        {
            return false;
        }

        return InOrderBy(cards, High, 1, 2, (int)Rank.Ace)
            || InOrderBy(cards, High, -1, 2, (int)Rank.Ace)
            || InOrderBy(cards, Low, 1, 1, (int)Rank.King)
            || InOrderBy(cards, Low, -1, 1, (int)Rank.King);
    }

    private static int High(Card card) => (int)((Card.Suited)card).Rank;

    /// <summary>The ace read as one, which is the only way A-2-3 is in order.</summary>
    private static int Low(Card card) => High(card) == (int)Rank.Ace ? 1 : High(card);

    /// <summary>
    /// Anchored on the first card that names itself, since a joker names
    /// nothing and takes whatever rank the place it sits in asks for.
    /// </summary>
    private static bool InOrderBy(
        IReadOnlyList<Card> cards,
        Func<Card, int> rank,
        int step,
        int lowest,
        int highest)
    {
        var anchor = cards.ToList().FindIndex(card => card is Card.Suited);
        var from = rank(cards[anchor]);

        return cards.Select((card, at) => (card, at)).All(place =>
        {
            var wanted = from + (step * (place.at - anchor));

            return wanted >= lowest
                && wanted <= highest
                && (place.card is Card.Joker || rank(place.card) == wanted);
        });
    }
}
