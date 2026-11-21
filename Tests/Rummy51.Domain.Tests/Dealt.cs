namespace Rummy51.Domain.Tests;

/// <summary>
/// A hand dealt from a deck laid out on purpose, so that a rule about what
/// somebody is holding can be held to cards they are actually holding.
///
/// Where those cards have to lie in the deck is asked of <see cref="Dealing"/>
/// rather than worked out here. The deal is the thing under test everywhere
/// else, and a second copy of its arithmetic living in the fixture would
/// agree with it exactly until the day one of the two changed - at which
/// point every test would go on passing against a deal nobody plays.
/// </summary>
internal static class Dealt
{
    /// <summary>
    /// Cutting at nought takes the card lying there as the finishing card
    /// and deals from the next one, so everything is laid out one along.
    /// A joker there would be taken under rule 2.3 and move the rest.
    /// </summary>
    private const int Cut = 0;

    /// <summary>In play, with the opener holding exactly what was asked for.</summary>
    public static Hand Holding(IReadOnlyList<Card> opener, int seats = 2, int jokers = 2)
    {
        var hand = new Hand(seats, dealerSeat: 0, LaidOut(opener, seats, jokers));

        hand.CutAt(Cut);
        hand.Deal();
        hand.Begin();

        return hand;
    }

    /// <summary>In play, dealt from a shuffle, which is every other test.</summary>
    public static Hand Shuffled(int seats = 2, int jokers = 2, int seed = 4242, int dealerSeat = 0)
    {
        var hand = new Hand(seats, jokers, dealerSeat, seed);

        hand.CutAt(20);
        hand.Deal();
        hand.Begin();

        return hand;
    }

    /// <summary>Fifteen cards that go out in one strike, and one to throw.</summary>
    public static IReadOnlyList<Card> AWinningHand =>
    [
        Cards.S(4), Cards.S(5), Cards.S(6), Cards.S(7),
        Cards.H(9), Cards.H(10), Cards.H(Cards.Jack), Cards.H(Cards.Queen),
        Cards.C(Cards.King), Cards.D(Cards.King), Cards.H(Cards.King),
        Cards.S(8), Cards.D(8), Cards.H(8),
        Cards.C(2),
    ];

    private static Deck LaidOut(IReadOnlyList<Card> opener, int seats, int jokers)
    {
        var whole = (Deck.Packs * Suits * Ranks) + jokers;
        var playing = Enumerable.Repeat(true, seats).ToArray();
        var laid = new Card?[whole];

        foreach (var (card, at) in opener.Zip(WhereTheOpenersLand(playing, whole)))
        {
            laid[(Cut + 1 + at) % whole] = card;
        }

        Fill(laid, jokers);

        return Deck.Arranged([.. laid.Select(card => card!)], jokers);
    }

    /// <summary>
    /// Which cards of the deal reach the seat that opens, by the order they
    /// are taken off the deck. The passes say how many went where and in
    /// what order, which is all the slicing there is.
    /// </summary>
    private static IReadOnlyList<int> WhereTheOpenersLand(IReadOnlyList<bool> playing, int whole)
    {
        var nothing = Enumerable.Repeat(Cards.C(2), whole).ToArray();
        var passes = Dealing.Deal(nothing, playing, dealerSeat: 0, alreadyHeld: 0, Dealing.FirstDeal)
            .Passes;

        var opener = Dealing.After(0, playing);
        var landed = new List<int>();
        var at = 0;

        foreach (var pass in passes)
        {
            if (pass.Seat == opener)
            {
                landed.AddRange(Enumerable.Range(at, pass.Count));
            }

            at += pass.Count;
        }

        return landed;
    }

    /// <summary>
    /// Everything the laid-out hand did not use, so the deck is two packs
    /// and its jokers and not a deck of convenience. The jokers go last and
    /// so never land on the cut, which rule 2.3 would otherwise answer.
    /// </summary>
    private static void Fill(Card?[] laid, int jokers)
    {
        var used = laid.OfType<Card>()
            .GroupBy(card => card.Code)
            .ToDictionary(same => same.Key, same => same.Count());

        var rest = new List<Card>();

        foreach (var card in Enumerable.Range(0, Deck.Packs).SelectMany(_ => Pack()))
        {
            if (used.TryGetValue(card.Code, out var spare) && spare > 0)
            {
                used[card.Code] = spare - 1;

                continue;
            }

            rest.Add(card);
        }

        rest.AddRange(Enumerable.Range(0, jokers).Select(at => new Card.Joker((at % 2) + 1)));

        var taken = 0;

        for (var at = 0; at < laid.Length; at++)
        {
            laid[at] ??= rest[taken++];
        }
    }

    private static int Suits => Enum.GetValues<Suit>().Length;

    private static int Ranks => Enum.GetValues<Rank>().Length;

    private static IEnumerable<Card> Pack() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select (Card)new Card.Suited(suit, rank);
}
