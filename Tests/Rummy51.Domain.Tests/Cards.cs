namespace Rummy51.Domain.Tests;

/// <summary>
/// Cards written the way the rules talk about them, so that a test reads as
/// the sentence it is holding the code to and not as a constructor. The
/// rules say Q-K-A; so does the test that holds the code to it.
/// </summary>
internal static class Cards
{
    public static Card S(int rank) => new Card.Suited(Suit.Spades, (Rank)rank);

    public static Card H(int rank) => new Card.Suited(Suit.Hearts, (Rank)rank);

    public static Card D(int rank) => new Card.Suited(Suit.Diamonds, (Rank)rank);

    public static Card C(int rank) => new Card.Suited(Suit.Clubs, (Rank)rank);

    /// <summary>Rule 1.1: the face picks the picture and means nothing in play.</summary>
    public static Card Joker(int face = 1) => new Card.Joker(face);

    public const int Ace = 14;
    public const int King = 13;
    public const int Queen = 12;
    public const int Jack = 11;
}
