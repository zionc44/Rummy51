using Rummy51.Application.Authentication;
using Rummy51.Application.Tables;
using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

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
    private Hand? hand;

    public OpenTable(CreateTableRequest request)
    {
        // Version 7 is time-ordered, so listing tables by id lists them in the
        // order they were opened.
        Id = Guid.CreateVersion7();
        Mode = request.Mode;
        Limit = request.Limit;
        Jokers = request.Jokers;
        seats = new PlayerSummary?[request.Seats];
    }

    public Guid Id { get; }
    public GameMode Mode { get; }
    public int? Limit { get; }
    public int Jokers { get; }

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

        if (Started)
        {
            // Filling the table is what starts the hand. There is no button
            // and nobody waits for anybody.
            hand = new Hand(seats.Length, Jokers, Random.Shared.Next(seats.Length), Random.Shared.Next());
        }
    }

    public void Unseat(Guid playerId)
    {
        var held = Array.FindIndex(seats, seat => seat?.PlayerId == playerId);

        if (held != -1)
        {
            seats[held] = null;
        }
    }

    /// <summary>Null until the table has filled and a hand has begun.</summary>
    public int? CutterSeat => hand?.CutterSeat;

    public bool AwaitingCut => hand?.Phase == HandPhase.Cutting;

    public int SeatOf(Guid playerId) =>
        Array.FindIndex(seats, seat => seat?.PlayerId == playerId);

    public bool CutAt(int position) => hand?.CutAt(position) ?? false;

    public HandView? DescribeHand() =>
        hand is null
            ? null
            : new HandView(
                hand.Phase,
                hand.DealerSeat,
                hand.CutterSeat,
                hand.DeckSize,
                hand.CutPosition,
                [.. hand.Backs.Select(back => $"{back}B")],
                [.. hand.RevealedAtCut.Select(card => card.Code)],
                hand.FinishingCard?.Code);

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, Jokers, [.. seats.Select(Occupant)], Started);

    private static TableSeat Occupant(PlayerSummary? player) =>
        player is null
            ? new TableSeat(null, null, null)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl);
}
