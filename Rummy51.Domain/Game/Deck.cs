namespace Rummy51.Domain.Game;

/// <summary>
/// The deck one hand is played from: shuffled from a seed, and then cut.
///
/// It is where everything about the cards is known before anybody holds
/// one - which pack each came out of, where the jokers are lying, and what
/// the cut turned up - and it stops mattering the moment the cards are
/// dealt. A hand keeps it so that the table can still be told how the hand
/// began.
/// </summary>
public sealed class Deck
{
    /// <summary>Rummy is played with two, always.</summary>
    public const int Packs = 2;

    private const int JokerFaces = 2;

    private IReadOnlyList<Card> cards;
    private Cut? cut;

    public Deck(int jokers, int seed)
    {
        Seed = seed;

        var fresh = New(jokers);
        var backs = BacksOf(jokers);

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

    /// <summary>How many cards were face down for the cutter to choose from.</summary>
    public int Size { get; }

    /// <summary>Which pack each card came out of, by position in the deck.</summary>
    public IReadOnlyList<int> Backs { get; }

    /// <summary>
    /// The one thing here nobody at the table may know. It reaches a client
    /// only where a development setting has asked for it, so that a cut onto
    /// a joker can be worked on without waiting a hundred hands for one.
    /// </summary>
    public IReadOnlyList<int> JokerPositions { get; }

    /// <summary>
    /// Where the cutter chose, counted from the top as the deck lay before
    /// them. Everyone at the table needs it: without it only the one who
    /// clicked knows which card turned over.
    /// </summary>
    public int? CutPosition { get; private set; }

    public Card? FinishingCard => cut?.FinishingCard;

    public IReadOnlyList<Card> RevealedAtCut => cut?.Revealed ?? [];

    /// <summary>What the deal will find, which is what the cut left.</summary>
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

    /// <summary>Fifty-two: four suits of thirteen.</summary>
    private static int PackSize => Enum.GetValues<Suit>().Length * Enum.GetValues<Rank>().Length;

    /// <summary>
    /// Two full packs and however many jokers the table was opened with, in
    /// the order a new deck comes in.
    /// </summary>
    private static IReadOnlyList<Card> New(int jokers) =>
    [
        .. Enumerable.Range(0, Packs).SelectMany(_ => Pack()),
        // Alternating faces, so two jokers is one of each and four is two of
        // each, which is what two packs would actually give you.
        .. Enumerable.Range(0, jokers).Select(index => new Card.Joker(index % JokerFaces + 1))
    ];

    /// <summary>
    /// Shuffled by a seed rather than by chance, so a hand that behaved oddly
    /// can be dealt again exactly as it was.
    /// </summary>
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

    /// <summary>
    /// The back each card in a new deck wears: one pack's, then the other's.
    /// Two cards that look alike came from different packs, and in a real
    /// deck they really do have different backs - which is also why the split
    /// is exactly even rather than a coin tossed for every card.
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
