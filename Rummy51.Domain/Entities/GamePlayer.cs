namespace Rummy51.Domain.Entities;

/// <summary>
/// Rule 10.4: who sat where. It belongs to the game and not to each hand,
/// because a score carried across hands cannot be handed to somebody else.
/// </summary>
public class GamePlayer
{
    public Guid GameId { get; set; }

    public Guid PlayerId { get; set; }

    /// <summary>Rules 2.1, 2.2 and 2.4 all count in these.</summary>
    public int Seat { get; set; }
}
