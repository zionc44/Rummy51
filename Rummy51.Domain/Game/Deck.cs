namespace Rummy51.Domain.Game;

/// <summary>
/// The deck one hand is played from: shuffled from a seed, and then cut. It
/// is where everything about the cards is known before anybody holds one,
/// and it stops mattering the moment they are dealt - a hand keeps it so the
/// table can still be told how the hand began.
/// </summary>
public sealed class Deck
{
    /// <summary>Rule 1.1.</summary>
    public const int Packs = 2;

    private const int JokerFaces = 2;

    private IReadOnlyList<Card> cards;
    private Cut? cut;
    private bool finishingTaken;

    public Deck(int jokers, int seed)
        : this(New(jokers), BacksOf(jokers), seed)
    {
    }

    /// <summary>
    /// Rule 12.6: made from whatever the table was not keeping. The backs are
    /// given out afresh, nothing having carried one across - and nothing can
    /// tell, a back being seen only face down over a card nobody was shown.
    /// </summary>
    public Deck(IReadOnlyList<Card> cards, int seed)
        : this(cards, BacksFor(cards.Count), seed)
    {
    }

    private Deck(IReadOnlyList<Card> fresh, IReadOnlyList<int> backs, int seed)
    {
        Seed = seed;

        // One shuffle, applied to both, so every card keeps the back of the
        // pack it came from. Shuffling the backs separately would be a lie
        // about what two packs are, and would not come out even.
        var order = Shuffle<int>([.. Enumerable.Range(0, fresh.Count)], seed);

        cards = [.. order.Select(index => fresh[index])];
        Backs = [.. order.Select(index => backs[index])];
        Size = cards.Count;

        // Taken now, because the cut rearranges the deck and would move them.
        JokerPositions =
            [.. cards.Select((card, at) => (card, at)).Where(e => e.card is Card.Joker).Select(e => e.at)];
    }

    /// <summary>Kept so a hand that behaved oddly can be dealt again exactly.</summary>
    public int Seed { get; }

    public int Size { get; }

    public IReadOnlyList<int> Backs { get; }

    /// <summary>
    /// The one thing here nobody at the table may know. It reaches a client
    /// only where a development setting has asked for it, a cut onto a joker
    /// being otherwise one hand in a hundred to work on.
    /// </summary>
    public IReadOnlyList<int> JokerPositions { get; }

    /// <summary>Without it only the one who clicked knows which card turned.</summary>
    public int? CutPosition { get; private set; }

    /// <summary>Rule 2.2, and null once taken under 7.1.</summary>
    public Card? FinishingCard => finishingTaken ? null : cut?.FinishingCard;

    /// <summary>Rule 7.1.</summary>
    public Card? TakeTheFinishingCard()
    {
        if (FinishingCard is not { } card)
        {
            return null;
        }

        finishingTaken = true;

        return card;
    }

    /// <summary>Rule 7.4.</summary>
    public void GiveTheFinishingCardBack() => finishingTaken = false;

    public IReadOnlyList<Card> RevealedAtCut => cut?.Revealed ?? [];

    public IReadOnlyList<Card> Remaining => cards;

    /// <summary>Cut once and never again; a second cut is not a cut.</summary>
    public void CutAt(int position)
    {
        if (cut is not null)
        {
            return;
        }

        cut = Cutting.At(cards, position);
        CutPosition = ((position % Size) + Size) % Size;
        cards = cut.Remaining;
    }

    private static int PackSize => Enum.GetValues<Suit>().Length * Enum.GetValues<Rank>().Length;

    /// <summary>Rule 1.1, in the order a new deck comes in.</summary>
    private static IReadOnlyList<Card> New(int jokers) =>
    [
        .. Enumerable.Range(0, Packs).SelectMany(_ => Pack()),
        // Alternating faces, so two jokers is one of each and four is two of
        // each, which is what two packs would actually give you.
        .. Enumerable.Range(0, jokers).Select(index => new Card.Joker(index % JokerFaces + 1))
    ];

    /// <summary>By a seed rather than by chance, so a hand can be dealt again.</summary>
    private static IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> cards, int seed)
    {
        var shuffled = cards.ToArray();
        var random = new Random(seed);

        // Fisher-Yates: every ordering equally likely, which the obvious
        // sort-by-random-key does not quite manage.
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        return shuffled;
    }

    /// <summary>Half of one pack's and half of the other's, whatever is left.</summary>
    private static IReadOnlyList<int> BacksFor(int count) =>
        [.. Enumerable.Range(0, count).Select(at => at < count / 2 ? 1 : 2)];

    /// <summary>
    /// One pack's, then the other's: two cards that look alike really do
    /// have different backs, so the split is even and not a coin toss.
    /// </summary>
    private static IReadOnlyList<int> BacksOf(int jokers) =>
    [
        .. Enumerable.Range(0, Packs).SelectMany(pack => Enumerable.Repeat(pack + 1, PackSize)),
        .. Enumerable.Range(0, jokers).Select(index => index % JokerFaces + 1)
    ];

    private static IEnumerable<Card> Pack() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select new Card.Suited(suit, rank);
}
