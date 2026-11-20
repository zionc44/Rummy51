namespace Rummy51.Infrastructure;

/// <summary>
/// Aids for testing a game by hand. They are bound only when the host is
/// running in development, so a section of these in a deployed configuration
/// is read by nobody, and every one of them is off until it is asked for.
/// </summary>
public sealed class TestingSettings
{
    public const string SectionName = "Testing";

    /// <summary>
    /// Breaks rule 11.5 on purpose, a joker at the cut being far too rare to
    /// meet by playing. Plainly a cheat, which is why it is here.
    /// </summary>
    public bool MarkJokers { get; init; }

    /// <summary>
    /// Deals the first hand over until somebody holds rule 12.9's four
    /// pairs, which is otherwise about one hand in a hundred and twenty.
    /// </summary>
    public bool FourPairs { get; init; }

    /// <summary>
    /// Deals the player after the dealer a hand of nothing but melds, so
    /// that a hand can be taken to its end on purpose rather than waited
    /// for. Everything after the deal is the real game. It does this on
    /// every deal, a redeal included, which is what makes rule 8.2's
    /// doubling reachable by hand - at the cost of rule 12.7, since what
    /// a redeal then deals is a whole hand and not twelve on top of three.
    /// </summary>
    public bool ArrangedHand { get; init; }
}
