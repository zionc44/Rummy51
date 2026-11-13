namespace Rummy51.Domain.Entities;

/// <summary>
/// Rule 9.2, chosen when a table is opened and never changed: one hand,
/// hands until somebody crosses a score, or a fixed number of them.
/// </summary>
public enum GameMode
{
    Single,

    Survival,

    Series
}
