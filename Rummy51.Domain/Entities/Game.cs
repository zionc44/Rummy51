namespace Rummy51.Domain.Entities;

/// <summary>
/// Rules 9.1 and 9.2. The table it was played at lives in memory and does
/// not outlast it, so what it was opened for is kept here rather than a
/// reference to something that will be gone.
/// </summary>
public class Game
{
    public Guid Id { get; set; }

    public Guid TableId { get; set; }

    public GameMode Mode { get; set; }

    /// <summary>Rule 9.2: points or hands, and null for a single hand.</summary>
    public int? Target { get; set; }

    public int Seats { get; set; }

    /// <summary>Rule 1.1.</summary>
    public int Jokers { get; set; }

    /// <summary>
    /// Whoever the mode's own ending left in front, which is not the winner
    /// of any one hand and is not a sum anybody can take for granted. Null
    /// until the game ends.
    /// </summary>
    public Guid? WinnerId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }
}
