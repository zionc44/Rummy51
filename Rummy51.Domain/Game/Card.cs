namespace Rummy51.Domain.Game;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

/// <summary>The ace sits at the top. Rule 1.2.</summary>
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
/// Rule 1.1 puts two of every card in the deck, so a held card carries a
/// number given when it reached its holder and kept until it leaves. It is
/// what a hand is rearranged by.
/// </summary>
public readonly record struct HeldCard(int Id, Card Card, int Owner = -1)
{
    /// <summary>Never laid, so rule 12.5 has not fixed whose it is.</summary>
    public const int Nobody = -1;
}

/// <summary>
/// One of the fifty-two or a joker, and nothing else. Making the joker a
/// fifth suit, or a fifteenth rank, would be a lie that every run and every
/// count would then have to work around.
/// </summary>
public abstract record Card
{
    private Card()
    {
    }

    /// <summary>What crosses the wire, and what the picture is drawn from.</summary>
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

    /// <summary><paramref name="Face"/> picks the picture and means nothing in play.</summary>
    public sealed record Joker(int Face) : Card
    {
        public override string Code => $"J-{Face:00}";
    }
}
