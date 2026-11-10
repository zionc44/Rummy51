namespace Rummy51.Domain.Game;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

/// <summary>
/// The ace sits at the top because it is worth eleven wherever it is used,
/// including the low run A-2-3. Nothing here is worth one.
/// </summary>
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

/// <summary>
/// A card while somebody is holding it, carrying the one thing that tells it
/// from its twin in the other pack: a number given when it reached them and
/// kept until it leaves. Two sevens of hearts are the same card and not the
/// same thing, and a hand being rearranged has to know the difference.
/// </summary>
public readonly record struct HeldCard(int Id, Card Card);

/// <summary>
/// A card is one of the fifty-two or a joker, and nothing else. Making the
/// joker a fifth suit, or a fifteenth rank, would be a lie that every run and
/// every count would then have to work around.
/// </summary>
public abstract record Card
{
    private Card()
    {
    }

    /// <summary>
    /// A short, stable name for the card. It is what crosses the wire, and it
    /// is also what the client draws the picture from, so one name serves
    /// both and the two cannot drift apart.
    /// </summary>
    public abstract string Code { get; }

    public sealed record Suited(Suit Suit, Rank Rank) : Card
    {
        public override string Code => $"{Letter(Suit)}-{(int)Rank:00}";

        private static char Letter(Suit suit) => suit switch
        {
            Suit.Clubs => 'C',
            Suit.Diamonds => 'D',
            Suit.Hearts => 'H',
            _ => 'S'
        };
    }

    /// <summary>
    /// <paramref name="Face"/> says which of the two jokers to draw and
    /// carries no meaning in play: the two are interchangeable.
    /// </summary>
    public sealed record Joker(int Face) : Card
    {
        public override string Code => $"J-{Face:00}";
    }
}
