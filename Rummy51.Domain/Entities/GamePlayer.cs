namespace Rummy51.Domain.Entities;

/// <summary>
/// Rule 10.4: who sat where, and what the game came to for them.
/// It belongs to the game and not to each hand, because a score carried
/// across hands cannot be handed to somebody else.
///
/// Everything below the seat could be added up again out of the hands, and
/// is written down all the same, for the reason <see cref="HandResult"/>
/// keeps its score: rules 9.4 and 9.5 may yet change, and a game already
/// played must keep the standing it was played to. The hands remain the
/// evidence it was arrived at from. All of it is nought until the game
/// ends, because until then there is nothing it came to.
/// </summary>
public class GamePlayer
{
    public Guid GameId { get; set; }

    public Guid PlayerId { get; set; }

    /// <summary>Rules 2.1, 2.2 and 2.4 all count in these.</summary>
    public int Seat { get; set; }

    /// <summary>Rule 9.5 reads the order they went out in off this.</summary>
    public int HandsPlayed { get; set; }

    /// <summary>Rule 8.1 added up across the game, signed.</summary>
    public int Total { get; set; }

    /// <summary>
    /// Rule 9.4: the hand that put them out, and null for whoever was
    /// never put out - the winner of a survival game, and everybody in a
    /// game that has no such ending.
    /// </summary>
    public int? OutAfterHand { get; set; }

    /// <summary>Rule 9.5, or rule 8.3 where the game was a single hand.</summary>
    public int Place { get; set; }
}
