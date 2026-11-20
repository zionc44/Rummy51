using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Not a rule. A hand that can be gone out with is one deal in some
/// enormous number, so there is no searching for one: this lays the deck
/// out so that one seat is dealt melds. Everything after that is the real
/// game - rules 5.1, 6.1, 6.4 and 6.5 all still have to be satisfied by
/// whoever is holding it.
///
/// The cut is what makes this possible to aim. It rotates the deck by the
/// position the cutter chose and takes one card off the top, so the card
/// the deal reaches first is known before the deck is built.
/// </summary>
internal static class ArrangedDeal
{
    private const int PerPass = 3;
    private const int FewerInTheLast = 2;

    /// <summary>
    /// Fourteen cards that are nothing but melds, and one that is not. The
    /// run of spades is the clean one rule 5.1 asks for, and the four
    /// together come to well past its fifty-one.
    /// </summary>
    private static IReadOnlyList<Card> Winning =>
    [
        Card(Suit.Spades, Rank.Four), Card(Suit.Spades, Rank.Five),
        Card(Suit.Spades, Rank.Six), Card(Suit.Spades, Rank.Seven),

        Card(Suit.Hearts, Rank.Nine), Card(Suit.Hearts, Rank.Ten),
        Card(Suit.Hearts, Rank.Jack), Card(Suit.Hearts, Rank.Queen),

        Card(Suit.Clubs, Rank.King), Card(Suit.Diamonds, Rank.King),
        Card(Suit.Hearts, Rank.King),

        Card(Suit.Spades, Rank.Eight), Card(Suit.Diamonds, Rank.Eight),
        Card(Suit.Hearts, Rank.Eight),

        // The one that is not, so there is something to throw.
        Card(Suit.Clubs, Rank.Two),
    ];

    /// <summary>
    /// The same hand, dealt to the player after the dealer. They open, so
    /// they are the seat somebody testing will be watching.
    /// </summary>
    public static Hand For(Hand was, int seats, int jokers, int position)
    {
        var whole = Deck.Packs * Suits() * Ranks() + jokers;
        var first = (was.DealerSeat + 1) % seats;
        var landing = WhereTheyLand(seats, was.DealerSeat, first);
        var laid = new Card?[whole];

        // The cut rotates to `position` and takes that card as the finishing
        // one, so the deal starts at the card after it and wraps.
        foreach (var (card, at) in Winning.Zip(landing))
        {
            laid[(position + 1 + at) % whole] = card;
        }

        Fill(laid, Rest(laid, jokers), position);

        // Rule 8.2 reads the redeal counts at the end of the hand, and this
        // is the one road to a hand that has been redealt and then won.
        return new Hand(
            seats,
            was.DealerSeat,
            Deck.Arranged([.. laid.Select(card => card!)], jokers),
            was.AgreedRedeals,
            was.ForcedRedeals);
    }

    /// <summary>
    /// Which cards of the deal land at one seat, by the order they are
    /// taken. It mirrors <see cref="Dealing"/> and is checked against it by
    /// the harness rather than trusted.
    /// </summary>
    private static IReadOnlyList<int> WhereTheyLand(int seats, int dealerSeat, int seat)
    {
        var first = (dealerSeat + 1) % seats;
        var landed = new List<int>();
        var at = 0;

        foreach (var round in Enumerable.Range(0, Dealing.FirstDeal))
        {
            foreach (var step in Enumerable.Range(0, seats))
            {
                var who = (first + step) % seats;
                var count = round == Dealing.FirstDeal - 1 && who != first ? FewerInTheLast : PerPass;

                if (who == seat)
                {
                    landed.AddRange(Enumerable.Range(at, count));
                }

                at += count;
            }
        }

        return landed;
    }

    /// <summary>Everything the arranged hand did not use, jokers last.</summary>
    private static List<Card> Rest(Card?[] laid, int jokers)
    {
        var used = laid.OfType<Card>().GroupBy(card => card.Code).ToDictionary(same => same.Key, same => same.Count());
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

        rest.AddRange(Enumerable.Range(0, jokers).Select(index => new Card.Joker(index % 2 + 1)));

        return rest;
    }

    /// <summary>
    /// Rule 2.3 would give the cutter a joker found at the cut, and every
    /// one of those is a card somebody is not dealt - which would move the
    /// whole deal along and put the arranged hand somewhere else. So the
    /// cut lands on something that is not a joker.
    /// </summary>
    private static void Fill(Card?[] laid, List<Card> rest, int position)
    {
        var jokers = rest.Where(card => card is Card.Joker).ToList();
        var plain = rest.Where(card => card is Card.Suited).ToList();

        if (laid[position] is null && plain.Count > 0)
        {
            laid[position] = plain[^1];
            plain.RemoveAt(plain.Count - 1);
        }

        var next = plain.Concat(jokers).ToList();
        var taken = 0;

        for (var at = 0; at < laid.Length; at++)
        {
            laid[at] ??= next[taken++];
        }
    }

    private static Card.Suited Card(Suit suit, Rank rank) => new(suit, rank);

    private static IEnumerable<Card> Pack() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select Card(suit, rank);

    private static int Suits() => Enum.GetValues<Suit>().Length;

    private static int Ranks() => Enum.GetValues<Rank>().Length;
}
