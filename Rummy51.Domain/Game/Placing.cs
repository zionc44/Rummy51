namespace Rummy51.Domain.Game;

/// <summary>Rules 8.3 and 9.5: where a hand left them, and where a game did.</summary>
public static class Placing
{
    /// <summary>
    /// Where each score comes, by the seat that holds it. Equal scores take
    /// the same place and the next one along skips what they took between
    /// them, so a shared second leaves third empty rather than filled.
    /// </summary>
    public static IReadOnlyList<int> Of(IReadOnlyList<int> scores) =>
        Of(scores, [.. scores.Select(_ => true)]);

    /// <summary>
    /// The same among some of the seats. A seat that was not in the hand
    /// has no place in it, which is nought rather than last: it did not
    /// come anywhere, and placing it would put it ahead of everybody who
    /// played and lost.
    /// </summary>
    public static IReadOnlyList<int> Of(IReadOnlyList<int> scores, IReadOnlyList<bool> among)
    {
        var lowestFirst = scores.Where((_, seat) => among[seat]).Order().ToArray();

        return
        [
            .. scores.Select((score, seat) =>
                among[seat] ? Array.IndexOf(lowestFirst, score) + 1 : 0)
        ];
    }

    /// <summary>
    /// Rule 9.5: where a game left them. Lasting is the whole of what it
    /// asks, so more hands behind a seat is a higher place whatever anybody
    /// scored, and only two who went out in the same hand are split by what
    /// they had. Two alike in both share a place, as they do under 8.3.
    ///
    /// Nothing here puts the winner first. It does not have to: the last
    /// one in has played every hand and is the only seat under the number,
    /// so both keys already have them above everybody else.
    /// </summary>
    public static IReadOnlyList<int> InTheGame(
        IReadOnlyList<int> hands,
        IReadOnlyList<int> totals,
        IReadOnlyList<bool> among)
    {
        var lastOutFirst = Enumerable.Range(0, hands.Count)
            .Where(seat => among[seat])
            .Select(seat => (hands[seat], totals[seat]))
            .OrderByDescending(seat => seat.Item1)
            .ThenBy(seat => seat.Item2)
            .ToArray();

        return
        [
            .. Enumerable.Range(0, hands.Count).Select(seat =>
                among[seat]
                    ? Array.IndexOf(lastOutFirst, (hands[seat], totals[seat])) + 1
                    : 0)
        ];
    }
}
