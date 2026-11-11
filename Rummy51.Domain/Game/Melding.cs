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

    /// <summary>What a first showing has to come to.</summary>
    private const int FiftyOne = 51;

    /// <summary>
    /// Whether what a player has laid is a showing they can stand behind:
    /// fifty-one between all of it, and a clean run somewhere among it. It
    /// is asked of everything laid in one turn rather than of each run on
    /// its own, because the turn is the move.
    /// </summary>
    public static bool IsAShowing(IReadOnlyList<IReadOnlyList<Card>> laid) =>
        laid.Any(HasCleanRun) && ShowingWorth(laid) >= FiftyOne;

    /// <summary>
    /// What a showing comes to. Every meld counts for what it is worth, a
    /// joker counting as the card it stands in for - except the one run
    /// providing the clean three, which counts only its clean part.
    ///
    /// Which run provides it is whichever costs least to give up, and a run
    /// with no joker costs nothing at all: it is clean end to end, so a
    /// showing that holds one is never docked. Only a player whose clean
    /// three is buried inside a run with a joker pays for it.
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

    /// <summary>
    /// Whether this meld holds a clean run: three or more in a row with no
    /// joker among them. A run with no joker at all is clean from end to
    /// end; one with a joker holds a clean run only where three of its
    /// cards still sit together. 6-7-8-joker-10 holds one and
    /// 5-6-joker-8-9 does not.
    /// </summary>
    public static bool HasCleanRun(IReadOnlyList<Card> meld) => CleanRunIn(meld).Count >= Least;

    /// <summary>
    /// What a meld lying on the table is worth. Every card counts for
    /// itself, and a joker counts for the card it is standing in for: in a
    /// set that is the rank, and in a run it is whatever the place it fills
    /// asks for.
    /// </summary>
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
    /// The rank each place in a run asks for, jokers included, read off
    /// whichever way the run is in order. Empty where it is no run.
    /// </summary>
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

    /// <summary>
    /// A place in a run read as a card. One is the ace read low, which is
    /// the only way A-2-3 is in order, and the ace is eleven either way.
    /// </summary>
    private static int WorthOfPlace(int rank) =>
        Worth.OfRank(rank == 1 ? Rank.Ace : (Rank)rank);

    /// <summary>
    /// The longest stretch of the meld with no joker in it, as cards. Only a
    /// run has one worth asking about; it is asked of anything and answers
    /// honestly either way.
    /// </summary>
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
