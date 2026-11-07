namespace Rummy51.Domain.Game;

public static class Deck
{
    /// <summary>Rummy is played with two, always.</summary>
    public const int Packs = 2;

    private const int JokerFaces = 2;

    /// <summary>Fifty-two: four suits of thirteen.</summary>
    public static int PackSize => Enum.GetValues<Suit>().Length * Enum.GetValues<Rank>().Length;

    /// <summary>
    /// Two full packs and however many jokers the table was opened with, in
    /// the order a new deck comes in.
    /// </summary>
    public static IReadOnlyList<Card> New(int jokers) =>
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
    public static IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> cards, int seed)
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
    public static IReadOnlyList<int> Backs(int jokers) =>
    [
        .. Enumerable.Range(0, Packs).SelectMany(pack => Enumerable.Repeat(pack + 1, PackSize)),
        .. Enumerable.Range(0, jokers).Select(index => index % JokerFaces + 1)
    ];

    private static IEnumerable<Card> Pack() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select new Card.Suited(suit, rank);
}
