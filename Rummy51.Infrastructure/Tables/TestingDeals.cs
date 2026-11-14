using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// The one place a deal is arranged, and only where a development setting
/// has asked for it. It is here rather than on the table so that the table
/// holds no cheats and this file holds nothing else.
/// </summary>
internal static class TestingDeals
{
    /// <summary>How many deals are looked through before giving up on the aid.</summary>
    private const int Shuffles = 20_000;

    /// <summary>
    /// Rule 12.9's four pairs come up about one deal in a hundred and
    /// twenty, which is a long evening to spend meeting them once. The cut
    /// is the cutter's own, since the cut is what decides which cards go
    /// where, and nothing about the deal this stops on is arranged: it is a
    /// shuffle like any other, and the cheat is only how many came first.
    /// </summary>
    public static Hand ShuffledUntilFourPairs(Hand was, int seats, int jokers, int position)
    {
        for (var tried = 0; tried < Shuffles; tried++)
        {
            var dealt = new Hand(seats, jokers, was.DealerSeat, Random.Shared.Next());

            dealt.CutAt(position);
            dealt.Deal();
            dealt.Begin();

            if (Enumerable.Range(0, seats).Any(dealt.CanForceARedeal))
            {
                return new Hand(seats, jokers, was.DealerSeat, dealt.Deck.Seed);
            }
        }

        return was;
    }
}
