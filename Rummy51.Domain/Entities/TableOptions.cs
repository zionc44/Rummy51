namespace Rummy51.Domain.Entities;

/// <summary>
/// Rules 1.1 and 9.2. The form offers exactly these, but anything can be
/// posted, so this is where it is decided.
/// </summary>
public static class TableOptions
{
    public static readonly IReadOnlyList<int> Seats = [2, 3, 4];

    public static readonly IReadOnlyList<int> Jokers = [0, 2, 4];

    /// <summary>Ceilings, not targets: a total climbs with bad play.</summary>
    public static readonly IReadOnlyList<int> SurvivalPoints = [100, 200, 500];

    /// <summary>
    /// By the number of players: rule 2.1 moves the deal on by one, so a
    /// series that did not divide would give somebody an extra deal.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> SeriesHands =
        new Dictionary<int, IReadOnlyList<int>>
        {
            [2] = [4, 6, 12],
            [3] = [3, 6, 12],
            [4] = [4, 8, 12]
        };

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
