namespace Rummy51.Domain.Game;

public enum MeldEnd
{
    Front,
    Back
}

/// <summary>
/// Chapters 4 and 5, enforced.
///
/// Where a card fits is asked rather than worked out: the card is put at one
/// end and then the other, and whatever is still a meld afterwards is a
/// place it fits. That way the ace, the jokers and the four suits of a set
/// are each answered once, here, and not again for every question about
/// them.
/// </summary>
public static class Melding
{
    /// <summary>Rule 4.1.</summary>
    private const int Least = 3;

    /// <summary>Rule 4.1.</summary>
    private const int Most = 4;

    /// <summary>Rule 5.1.</summary>
    private const int FiftyOne = 51;

    /// <summary>Rules 5.1, 5.6.</summary>
    public static bool IsAShowing(IReadOnlyList<IReadOnlyList<Card>> laid) =>
        laid.Any(HasCleanRun) && ShowingWorth(laid) >= FiftyOne;

    /// <summary>
    /// Rules 5.3, 5.4. Which run provides the clean three is whichever costs
    /// least to give up, and one with no joker costs nothing at all, being
    /// clean end to end.
    /// </summary>
    public static int ShowingWorth(IReadOnlyList<IReadOnlyList<Card>> laid)
    {
        var whole = laid.Sum(WorthOnTheTable);
        var providers = laid
            .Where(HasCleanRun)
            .Select(meld => WorthOnTheTable(meld) - Worth.Held(CleanRunIn(meld)))
            .ToList();

        return providers.Count == 0 ? whole : whole - providers.Min();
    }

    /// <summary>Rules 5.9, 5.10.</summary>
    public static IReadOnlyList<MeldEnd> WhereItFits(IReadOnlyList<Card> meld, Card card)
    {
        if (meld.Count < Least)
        {
            return [];
        }

        // Rule 5.10: a set has no ends, so offering two would be the same
        // place twice.
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
    /// Rule 5.11, asked the same way: the card is put in the joker's place,
    /// and if what is left is still a meld then that is the card the place
    /// was asking for all along.
    /// </summary>
    public static bool CanReplace(IReadOnlyList<Card> meld, int at, Card card)
    {
        // A joker for a joker is not a card coming off the table.
        if (at < 0 || at >= meld.Count || meld[at] is not Card.Joker || card is Card.Joker)
        {
            return false;
        }

        // Rule 5.11: a set gives up its joker only once it is whole, or
        // nothing was completed and nothing was paid.
        if (meld.Count < Most && IsSet(meld))
        {
            return false;
        }

        var swapped = meld.ToArray();

        swapped[at] = card;

        return IsMeld(swapped);
    }

    /// <summary>Rules 5.1, 5.2.</summary>
    public static bool HasCleanRun(IReadOnlyList<Card> meld) => CleanRunIn(meld).Count >= Least;

    /// <summary>Rules 1.3, 5.3.</summary>
    public static int WorthOnTheTable(IReadOnlyList<Card> meld)
    {
        if (!IsMeld(meld))
        {
            return 0;
        }

        return IsSet(meld)
            ? meld.Count * Worth.OfRank(meld.OfType<Card.Suited>().First().Rank)
            : PlacesOf(meld).Sum(WorthOfPlace);
    }

    /// <summary>Jokers included, read off whichever way the run is in order.</summary>
    private static IReadOnlyList<int> PlacesOf(IReadOnlyList<Card> run)
    {
        if (InOrderBy(run, High, 1, 2, (int)Rank.Ace))
        {
            return Places(run, High, 1);
        }

        if (InOrderBy(run, High, -1, 2, (int)Rank.Ace))
        {
            return Places(run, High, -1);
        }

        if (InOrderBy(run, Low, 1, 1, (int)Rank.King))
        {
            return Places(run, Low, 1);
        }

        return InOrderBy(run, Low, -1, 1, (int)Rank.King) ? Places(run, Low, -1) : [];
    }

    private static IReadOnlyList<int> Places(
        IReadOnlyList<Card> run,
        Func<Card, int> rank,
        int step)
    {
        var anchor = run.ToList().FindIndex(card => card is Card.Suited);
        var from = rank(run[anchor]);

        return [.. run.Select((_, at) => from + (step * (at - anchor)))];
    }

    /// <summary>Rules 1.2, 4.2: one is the ace read low, and worth eleven anyway.</summary>
    private static int WorthOfPlace(int rank) =>
        Worth.OfRank(rank == 1 ? Rank.Ace : (Rank)rank);

    /// <summary>Rule 5.2. Asked of anything, and honest either way.</summary>
    private static IReadOnlyList<Card> CleanRunIn(IReadOnlyList<Card> meld)
    {
        if (!IsRun(meld))
        {
            return [];
        }

        List<Card> longest = [];
        List<Card> running = [];

        foreach (var card in meld)
        {
            running = card is Card.Joker ? [] : [.. running, card];

            if (running.Count > longest.Count)
            {
                longest = running;
            }
        }

        return longest;
    }

    public static bool IsMeld(IReadOnlyList<Card> cards) =>
        cards.Count >= Least && (IsSet(cards) || IsRun(cards));

    /// <summary>Rule 4.1.</summary>
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

    /// <summary>Rules 4.1, 4.2 - which is why the ace is asked about twice.</summary>
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

    /// <summary>Rule 4.2.</summary>
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
