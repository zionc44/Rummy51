namespace Rummy51.Domain.Entities;

/// <summary>
/// What a table may be opened as. The form offers exactly these, but the form
/// is a convenience and anything can be posted, so this is where it is
/// decided.
/// </summary>
public static class TableOptions
{
    public static readonly IReadOnlyList<int> Seats = [2, 3, 4];

    /// <summary>
    /// How many jokers go into the deck. Even numbers only, because a deck is
    /// made of whole packs and each brings its pair.
    /// </summary>
    public static readonly IReadOnlyList<int> Jokers = [0, 2, 4];

    /// <summary>
    /// A score that ends the game. The winner of a hand takes minus thirty, so
    /// a total climbs with bad play: these are ceilings, not targets.
    /// </summary>
    public static readonly IReadOnlyList<int> SurvivalPoints = [100, 200, 500];

    /// <summary>
    /// How long a series runs, by the number of players. Every option divides
    /// by that number, because each player has to deal the same number of
    /// times — which is why there cannot be one list.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> SeriesHands =
        new Dictionary<int, IReadOnlyList<int>>
        {
            [2] = [4, 6, 12],
            [3] = [3, 6, 12],
            [4] = [4, 8, 12]
        };

    /// <summary>Whether a limit is one this mode offers at this many seats.</summary>
    public static bool Allows(GameMode mode, int seats, int? limit) => mode switch
    {
        // Nothing to run to: the hand ends and so does the game.
        GameMode.Single => limit is null,
        GameMode.Survival => limit is { } points && SurvivalPoints.Contains(points),
        GameMode.Series => limit is { } hands
            && SeriesHands.TryGetValue(seats, out var offered)
            && offered.Contains(hands),
        _ => false
    };
}
