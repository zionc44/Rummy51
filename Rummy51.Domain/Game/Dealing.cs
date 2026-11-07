namespace Rummy51.Domain.Game;

/// <summary>One pass of the deal: how many cards land at which seat.</summary>
public sealed record DealPass(int Seat, int Count);

/// <param name="Passes">The deal as the eye should see it, pass by pass.</param>
public sealed record HandDeal(
    IReadOnlyList<IReadOnlyList<Card>> Hands,
    IReadOnlyList<Card> Stock,
    IReadOnlyList<DealPass> Passes);

public static class Dealing
{
    private const int Rounds = 5;
    private const int PerPass = 3;

    /// <summary>What everyone but the first player gets in the last pass.</summary>
    private const int FewerInTheLast = 2;

    /// <param name="cards">The deck as the cut left it.</param>
    /// <param name="alreadyHeld">
    /// Cards the cutter is already holding from the cut. Each one is a card
    /// they are not dealt, so everybody ends the deal holding the same number.
    /// </param>
    public static HandDeal Deal(
        IReadOnlyList<Card> cards,
        int seats,
        int dealerSeat,
        int alreadyHeld)
    {
        if (seats is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(seats), "A table seats two to four.");
        }

        var cutter = Before(dealerSeat, seats);
        var first = After(dealerSeat, seats);

        var hands = Enumerable.Range(0, seats).Select(_ => new List<Card>()).ToArray();
        var passes = new List<DealPass>();
        var drawn = 0;
        var owed = alreadyHeld;

        foreach (var round in Enumerable.Range(0, Rounds))
        {
            foreach (var seat in Clockwise(first, seats))
            {
                var count = round == Rounds - 1 && seat != first ? FewerInTheLast : PerPass;

                if (seat == cutter && owed > 0)
                {
                    var spared = Math.Min(count, owed);
                    count -= spared;
                    owed -= spared;
                }

                hands[seat].AddRange(cards.Skip(drawn).Take(count));
                drawn += count;

                // A pass of nothing is not something to watch.
                if (count > 0)
                {
                    passes.Add(new DealPass(seat, count));
                }
            }
        }

        return new HandDeal(
            [.. hands.Select(IReadOnlyList<Card> (hand) => hand)],
            [.. cards.Skip(drawn)],
            passes);
    }

    /// <summary>Round the table from the first player, ending at the dealer.</summary>
    private static IEnumerable<int> Clockwise(int first, int seats) =>
        Enumerable.Range(0, seats).Select(step => (first + step) % seats);

    private static int After(int seat, int seats) => (seat + 1) % seats;

    private static int Before(int seat, int seats) => (seat + seats - 1) % seats;
}
