namespace Rummy51.Domain.Entities;

/// <summary>
/// What a table is playing for. Chosen when the table is opened and never
/// changed, because the scoring that follows from it starts with the first
/// hand.
/// </summary>
public enum GameMode
{
    /// <summary>One hand, scored, done.</summary>
    Single,

    /// <summary>Hands keep coming until a player crosses the agreed score.</summary>
    Survival,

    /// <summary>A fixed number of hands, with the results kept across them.</summary>
    Series
}
