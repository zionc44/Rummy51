using Rummy51.Application.Authentication;
using Rummy51.Application.Tables;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// A table while it exists, which is only ever in memory. Seats are an array
/// rather than a list because their positions are the point: the one who
/// opened the table holds the first, and leaving empties a seat instead of
/// moving everyone along.
/// </summary>
internal sealed class OpenTable
{
    private readonly PlayerSummary?[] seats;

    public OpenTable(CreateTableRequest request)
    {
        // Version 7 is time-ordered, so listing tables by id lists them in the
        // order they were opened.
        Id = Guid.CreateVersion7();
        Mode = request.Mode;
        Limit = request.Limit;
        seats = new PlayerSummary?[request.Seats];
    }

    public Guid Id { get; }
    public GameMode Mode { get; }
    public int? Limit { get; }

    /// <summary>
    /// Set the moment the last seat is taken, and never unset: a game that has
    /// begun is not waiting again because somebody walked away from it.
    /// </summary>
    public bool Started { get; private set; }

    public bool IsEmpty => seats.All(seat => seat is null);

    public void Seat(PlayerSummary player)
    {
        var free = Array.IndexOf(seats, null);

        if (free == -1)
        {
            return;
        }

        seats[free] = player;
        Started = seats.All(seat => seat is not null);
    }

    public void Unseat(Guid playerId)
    {
        var held = Array.FindIndex(seats, seat => seat?.PlayerId == playerId);

        if (held != -1)
        {
            seats[held] = null;
        }
    }

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, [.. seats.Select(Occupant)], Started);

    private static TableSeat Occupant(PlayerSummary? player) =>
        player is null
            ? new TableSeat(null, null, null)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl);
}
