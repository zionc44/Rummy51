namespace Rummy51.Domain.Game;

/// <summary>
/// What a series has made of one of its hands: rule 8.4's own doubling,
/// and whether 8.5 has sent a redeal's doubling onto it.
/// </summary>
public sealed record HandWeight(int Fixed, bool Redealt)
{
    /// <summary>Every hand outside a series, and a series hand nothing has touched.</summary>
    public static readonly HandWeight Single = new(1, false);

    public int Times => Fixed * (Redealt ? 2 : 1);

    /// <summary>Rule 8.5: a hand counting single, and so one a redeal falls on.</summary>
    public bool CountsSingle => Times == 1;
}

/// <summary>Rules 8.4 and 8.5: what each hand of a series counts for.</summary>
public static class Series
{
    /// <summary>Rule 8.4, by the hand's number in the series, counted from one.</summary>
    private static readonly IReadOnlyDictionary<int, (int[] Twice, int[] FourTimes)> Doubled =
        new Dictionary<int, (int[], int[])>
        {
            [3] = ([3], []),
            [4] = ([2], [4]),
            [6] = ([3], [6]),
            [8] = ([2, 6], [4, 8]),
            [12] = ([3, 9], [6, 12])
        };

    /// <summary>
    /// Every hand of a series of that length, given how many redeals the
    /// table agreed to in each hand played so far - the one being played
    /// included, which is what lets a hand still to come show what 8.5 has
    /// already sent onto it.
    /// </summary>
    public static IReadOnlyList<HandWeight> Weights(int hands, IReadOnlyList<int> agreedRedeals)
    {
        var weights = Enumerable.Range(1, hands)
            .Select(number => new HandWeight(FixedFor(hands, number), false))
            .ToArray();

        for (var at = 0; at < Math.Min(hands, agreedRedeals.Count); at++)
        {
            for (var redeal = 0; redeal < agreedRedeals[at]; redeal++)
            {
                FallFrom(weights, at);
            }
        }

        return weights;
    }

    /// <summary>Rule 8.5: the first hand from here still counting single, or none.</summary>
    private static void FallFrom(HandWeight[] weights, int at)
    {
        var single = Array.FindIndex(weights, at, weight => weight.CountsSingle);

        if (single != -1)
        {
            weights[single] = weights[single] with { Redealt = true };
        }
    }

    private static int FixedFor(int hands, int number)
    {
        if (!Doubled.TryGetValue(hands, out var doubled))
        {
            return 1;
        }

        return doubled.FourTimes.Contains(number) ? 4
            : doubled.Twice.Contains(number) ? 2
            : 1;
    }
}
