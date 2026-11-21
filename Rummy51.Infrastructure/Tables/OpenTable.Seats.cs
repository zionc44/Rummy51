using Rummy51.Application.Authentication;
using Rummy51.Application.Tables;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Who is sitting at the table and which of them are watching it, which
/// chapter 10 makes the same question asked twice. The hand beside this
/// never asks it: it plays whatever seat is in turn, and rule 3.6 covers
/// the one that is empty of a person but not of cards.
/// </summary>
internal sealed partial class OpenTable
{
    private readonly PlayerSummary?[] seats;

    /// <summary>When each seat's player stopped watching, and null is present.</summary>
    private readonly DateTimeOffset?[] gone;

    /// <summary>
    /// Whether a page is open on this table at that seat. Not the same
    /// question as the one above: rule 10.6 lets the table decide somebody
    /// is not watching while their page sits there waiting to be answered,
    /// and a table full of those is a table full of people.
    /// </summary>
    private readonly bool[] here;

    /// <summary>
    /// Set the moment the last seat is taken, and unset only where rule 9.3
    /// has emptied one again: a game that has begun is not waiting again
    /// because somebody walked away from it, but a game that is over leaves
    /// a table that may be a chair short.
    /// </summary>
    public bool Started { get; private set; }

    /// <summary>
    /// Rule 10.7: no page is open on this table, so there is nothing here
    /// that anybody is coming back to. A table with no seats taken at all
    /// answers the same way, which is the only answer it could give.
    ///
    /// It asks about pages and not about rule 10.6, because a table where
    /// everybody has let one wait run out is a table everybody is about to
    /// be asked a question at. Closing it would be answering for them.
    /// </summary>
    public bool Deserted =>
        Enumerable.Range(0, seats.Length).All(seat => seats[seat] is null || !here[seat]);

    public void Seat(PlayerSummary player)
    {
        var free = Array.IndexOf(seats, null);

        if (free == -1)
        {
            return;
        }

        seats[free] = player;
        here[free] = true;
        Started = seats.All(seat => seat is not null);

        if (Started)
        {
            // Filling the table is what starts the hand. There is no button
            // and nobody waits for anybody. Rule 9.3: a table that had a
            // chair to fill deals as a new one, the question rule 2.1
            // answers no longer being the same question.
            BeginAGame(Random.Shared.Next(seats.Length));
        }
    }

    /// <summary>
    /// Rule 10.1: once a hand is running, a seat is its player's whether or
    /// not they are watching, and rule 3.6 plays the turns they miss. An
    /// emptied seat would be worse than an absent player - the hand goes on
    /// holding their cards either way, and the next arrival would be dealt
    /// into the middle of somebody else's hand.
    ///
    /// Before a hand is running there is nothing to play out, so a chair
    /// nobody is sitting in is just a chair.
    ///
    /// Rules 10.2 and 10.3.
    /// </summary>
    public bool WentAway(Guid playerId)
    {
        var seat = SeatOf(playerId);

        if (seat == -1)
        {
            return false;
        }

        if (!Started)
        {
            seats[seat] = null;
            here[seat] = false;

            return true;
        }

        here[seat] = false;

        // Since when, so saying it twice does not move it.
        gone[seat] ??= clock.GetUtcNow();
        WaitAgainFor(seat);

        return true;
    }

    /// <summary>
    /// Rule 10.6. A wait that ran out is the table deciding the chair is
    /// empty of a person, and it may be right even where the page is still
    /// open: somebody can be in front of a screen without being at it.
    ///
    /// The wait is not touched. Whatever acts for them next is about to set
    /// one, and it will be short now for the same reason this was written.
    /// </summary>
    private void StoppedWatching(int seat)
    {
        if (seat != -1 && seats[seat] is not null)
        {
            gone[seat] ??= clock.GetUtcNow();
        }
    }

    /// <summary>Chapter 10, for whoever is looking at the table.</summary>
    public IReadOnlyList<bool> Absent => [.. gone.Select(since => since is not null)];

    public bool CameBack(Guid playerId)
    {
        var seat = SeatOf(playerId);

        if (seat == -1)
        {
            return false;
        }

        var answered = gone[seat] is not null;

        here[seat] = true;
        gone[seat] = null;

        if (answered)
        {
            WaitAgainFor(seat);
        }

        return answered;
    }

    public int SeatOf(Guid playerId) =>
        Array.FindIndex(seats, seat => seat?.PlayerId == playerId);

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, Jokers, [.. seats.Select(Occupant)], Started);

    /// <summary>Whoever rule 10.8 has finished with, since this was last asked.</summary>
    public IReadOnlyList<Guid> LetGo()
    {
        var taken = letGo.ToArray();

        letGo.Clear();

        return taken;
    }

    private TableSeat Occupant(PlayerSummary? player, int seat) =>
        player is null
            ? new TableSeat(null, null, null, false)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl, gone[seat] is not null);
}
