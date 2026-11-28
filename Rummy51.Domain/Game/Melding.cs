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

    /// <summary>Rule 4.2.</summary>
    private const int Lowest = 1;

    /// <summary>Rule 4.2, and where Rank.Ace already sits.</summary>
    private const int Highest = (int)Rank.Ace;

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

    /// <summary>Rules 1.3, 5.3, 5.5.</summary>
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

    /// <summary>
    /// Rules 4.1, 4.2, 4.3: the places a run fills, jokers included, and
    /// empty for anything that is not one. There is one scale and the ace
    /// answers at both ends of it, so nothing has to be told about corners
    /// - fifteen is off the scale wherever it is asked for.
    /// </summary>
    private static IReadOnlyList<int> PlacesOf(IReadOnlyList<Card> run)
    {
        var anchor = run.ToList().FindIndex(card => card is Card.Suited);

        if (anchor < 0)
        {
            return [];
        }

        // High before low, so an ace with nothing but jokers either side of
        // it is read at the top of the scale and not the bottom.
        foreach (var from in StartsAt((Card.Suited)run[anchor]))
        {
            foreach (var step in (int[])[1, -1])
            {
                var places = Places(run, anchor, from, step);

                if (InOrder(run, places))
                {
                    return places;
                }
            }
        }

        return [];
    }

    /// <summary>Rule 4.3.</summary>
    private static IReadOnlyList<int> StartsAt(Card.Suited named) =>
        named.Rank == Rank.Ace ? [Highest, Lowest] : [(int)named.Rank];

    private static IReadOnlyList<int> Places(
        IReadOnlyList<Card> run,
        int anchor,
        int from,
        int step) =>
        [.. run.Select((_, at) => from + (step * (at - anchor)))];

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

    /// <summary>Rules 4.1, 4.2, 4.3.</summary>
    private static bool IsRun(IReadOnlyList<Card> cards)
    {
        var named = cards.OfType<Card.Suited>().ToList();

        if (named.Count == 0 || named.Any(card => card.Suit != named[0].Suit))
        {
            return false;
        }

        return PlacesOf(cards).Count > 0;
    }

    /// <summary>A joker names nothing, so only the rest are asked anything.</summary>
    private static bool InOrder(IReadOnlyList<Card> run, IReadOnlyList<int> places) =>
        places.All(place => place is >= Lowest and <= Highest)
        && run.Select((card, at) => (card, at)).All(place =>
            place.card is Card.Joker || Sits((Card.Suited)place.card, places[place.at]));

    /// <summary>Rule 4.2: the ace answers for either end of the scale.</summary>
    private static bool Sits(Card.Suited card, int place) =>
        (int)card.Rank == place || (card.Rank == Rank.Ace && place == Lowest);
}
