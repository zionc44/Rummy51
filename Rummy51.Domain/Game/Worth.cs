namespace Rummy51.Domain.Game;

/// <summary>What a card counts for. Rules 1.2 and 1.3.</summary>
public static class Worth
{
    /// <summary>Rule 8.1.</summary>
    public const int NeverWentDown = 100;

    private const int Picture = 10;
    private const int Ace = 11;
    private const int JokerInHand = 20;

    public static int Held(Card card) => card switch
    {
        Card.Joker => JokerInHand,
        Card.Suited suited => OfRank(suited.Rank),
        _ => 0,
    };

    public static int Held(IEnumerable<Card> cards) => cards.Sum(Held);

    public static int OfRank(Rank rank) => rank switch
    {
        Rank.Ace => Ace,
        >= Rank.Jack => Picture,
        _ => (int)rank,
    };
}
