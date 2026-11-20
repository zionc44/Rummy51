namespace Rummy51.Domain.Game;

/// <summary>Rule 8.3.</summary>
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
}
