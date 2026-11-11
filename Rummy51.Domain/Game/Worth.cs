namespace Rummy51.Domain.Game;

/// <summary>
/// What a card counts for. Every number is worth itself, every picture ten,
/// and the ace eleven wherever it is used - which is why the ace is ranked
/// above the king and nothing here is worth one.
///
/// A joker is two different questions. In a hand it is twenty: a card
/// nobody got to use, and the most expensive thing to be caught holding. On
/// the table it is worth the card it is standing in for, because there it
/// is that card.
/// </summary>
public static class Worth
{
    /// <summary>
    /// What a hand costs its owner while they have never gone down, whatever
    /// they are holding. A hand nobody ever showed is not worth counting.
    /// </summary>
    public const int NeverWentDown = 100;

    private const int Picture = 10;
    private const int Ace = 11;
    private const int JokerInHand = 20;

    /// <summary>A card held in a hand, where a joker is worth twenty.</summary>
    public static int Held(Card card) => card switch
    {
        Card.Joker => JokerInHand,
        Card.Suited suited => OfRank(suited.Rank),
        _ => 0,
    };

    public static int Held(IEnumerable<Card> cards) => cards.Sum(Held);

    /// <summary>The ace eleven, every picture ten, every number itself.</summary>
    public static int OfRank(Rank rank) => rank switch
    {
        Rank.Ace => Ace,
        >= Rank.Jack => Picture,
        _ => (int)rank,
    };
}
