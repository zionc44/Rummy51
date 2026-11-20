namespace Rummy51.Domain.Game;

/// <summary>Rule 8.3.</summary>
public static class Placing
{
    /// <summary>
    /// Where each score comes, by the seat that holds it. Equal scores take
    /// the same place and the next one along skips what they took between
    /// them, so a shared second leaves third empty rather than filled.
    /// </summary>
    public static IReadOnlyList<int> Of(IReadOnlyList<int> scores)
    {
        var lowestFirst = scores.Order().ToArray();

        return [.. scores.Select(score => Array.IndexOf(lowestFirst, score) + 1)];
    }
}
