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
    /// <summary>Rule 2.4: fifteen and fourteen.</summary>
    public const int FirstDeal = 5;

    /// <summary>Rule 12.7: one round fewer, which is twelve and eleven.</summary>
    public const int Redeal = 4;

    private const int PerPass = 3;

    /// <summary>What everyone but the first player gets in the last pass.</summary>
    private const int FewerInTheLast = 2;

    /// <param name="cards">The deck as the cut left it.</param>
    /// <param name="playing">
    /// Which seats are in this hand, by their places at the table. A seat
    /// that is out keeps its place and is dealt nothing, so that the hands
    /// come back indexed the way everything else counts seats.
    /// </param>
    /// <param name="alreadyHeld">
    /// Cards the cutter is already holding from the cut. Each one is a card
    /// they are not dealt, so everybody ends the deal holding the same number.
    /// </param>
    /// <param name="rounds"><see cref="FirstDeal"/> or <see cref="Redeal"/>.</param>
    public static HandDeal Deal(
        IReadOnlyList<Card> cards,
        IReadOnlyList<bool> playing,
        int dealerSeat,
        int alreadyHeld,
        int rounds)
    {
        if (playing.Count(one => one) is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(playing), "A hand is dealt to two to four.");
        }

        var cutter = Before(dealerSeat, playing);
        var first = After(dealerSeat, playing);

        var hands = Enumerable.Range(0, playing.Count).Select(_ => new List<Card>()).ToArray();
        var passes = new List<DealPass>();
        var drawn = 0;
        var owed = alreadyHeld;

        foreach (var round in Enumerable.Range(0, rounds))
        {
            foreach (var seat in Clockwise(first, playing))
            {
                var count = round == rounds - 1 && seat != first ? FewerInTheLast : PerPass;

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
    public static IEnumerable<int> Clockwise(int first, IReadOnlyList<bool> playing)
    {
        var seat = first;

        do
        {
            yield return seat;
            seat = After(seat, playing);
        }
        while (seat != first);
    }

    /// <summary>
    /// The next seat in this hand, which is not always the next chair: a
    /// seat that is out keeps its place at the table and is stepped over.
    /// </summary>
    public static int After(int seat, IReadOnlyList<bool> playing) => Step(seat, playing, 1);

    public static int Before(int seat, IReadOnlyList<bool> playing) => Step(seat, playing, -1);

    private static int Step(int seat, IReadOnlyList<bool> playing, int by)
    {
        var at = seat;

        for (var tried = 0; tried < playing.Count; tried++)
        {
            at = (at + by + playing.Count) % playing.Count;

            if (playing[at])
            {
                return at;
            }
        }

        return seat;
    }
}
