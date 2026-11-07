namespace Rummy51.Domain.Game;

/// <param name="Revealed">
/// Jokers the cutter turned up. Theirs to keep, face up until play starts,
/// and each one costs them a card they would otherwise have been dealt.
/// </param>
/// <param name="Remaining">The deck as the deal will find it.</param>
public sealed record Cut(
    IReadOnlyList<Card> Revealed,
    Card FinishingCard,
    IReadOnlyList<Card> Remaining);

public static class Cutting
{
    /// <summary>
    /// The cutter picked a card. That card is the finishing card and whatever
    /// followed it is now in front, which is what cutting a deck does. A joker
    /// at that spot goes to them instead, and the next card is tried.
    /// </summary>
    public static Cut At(IReadOnlyList<Card> cards, int position)
    {
        var deck = Rotate(cards, position);
        var revealed = new List<Card>();
        var taken = 0;

        while (deck[taken] is Card.Joker joker)
        {
            revealed.Add(joker);
            taken++;
        }

        var finishing = deck[taken++];

        return new Cut(revealed, finishing, [.. deck.Skip(taken)]);
    }

    /// <summary>
    /// After a shuffle this changes nothing about what comes next, which is
    /// exactly why cutting is a ceremony. The players expect it, so it is
    /// performed.
    /// </summary>
    private static IReadOnlyList<Card> Rotate(IReadOnlyList<Card> cards, int position)
    {
        var at = ((position % cards.Count) + cards.Count) % cards.Count;

        return [.. cards.Skip(at), .. cards.Take(at)];
    }
}
