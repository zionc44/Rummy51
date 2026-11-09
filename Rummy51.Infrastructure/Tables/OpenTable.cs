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
    private readonly bool markJokers;
    private readonly TurnSettings turns;
    private readonly TimeProvider clock;
    private Hand? hand;
    private DateTimeOffset? dueAt;
    private TimeSpan waited;

    public OpenTable(
        CreateTableRequest request,
        bool markJokers,
        TurnSettings turns,
        TimeProvider clock)
    {
        this.markJokers = markJokers;
        this.turns = turns;
        this.clock = clock;

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
            WaitFor(TimeSpan.FromSeconds(turns.CutSeconds));
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

    /// <summary>
    /// The cut, and the deal that follows it. Nobody decides anything in
    /// between: at a table the cards are already going out.
    /// </summary>
    public bool CutAt(int position)
    {
        if (hand?.CutAt(position) != true)
        {
            return false;
        }

        hand.Deal();

        // Not a wait for anybody: the cards are going out, and the screen
        // takes a few seconds over what the server did at once.
        WaitFor(TimeSpan.FromSeconds(turns.DealSeconds));

        return true;
    }

    /// <summary>
    /// True once the table has waited as long as it said it would. The
    /// clock asks this of every table and acts on the ones that say yes.
    /// </summary>
    public bool IsOverdue => dueAt is { } due && clock.GetUtcNow() >= due;

    /// <summary>
    /// What the table does when nobody does it for themselves. A cut has to
    /// happen for the hand to go on and there is no wrong place to cut, so
    /// it is cut where chance says.
    /// </summary>
    public bool ActForWhoeverRanOut()
    {
        if (hand is null || !IsOverdue)
        {
            return false;
        }

        if (hand.Phase == HandPhase.Cutting)
        {
            // There is no wrong place to cut, so it is cut where chance says.
            return CutAt(Random.Shared.Next(hand.DeckSize));
        }

        if (hand.Phase == HandPhase.Dealing)
        {
            return BeginPlay();
        }

        // A turn that ran out. Drawing and discarding for whoever it was
        // lands here next; until then the wait simply ends.
        StopWaiting();

        return true;
    }

    private bool BeginPlay()
    {
        if (hand?.Begin() != true)
        {
            return false;
        }

        WaitFor(TimeSpan.FromSeconds(turns.FirstTurnSeconds));

        return true;
    }

    private void WaitFor(TimeSpan howLong)
    {
        waited = howLong;
        dueAt = clock.GetUtcNow() + howLong;
    }

    private void StopWaiting()
    {
        dueAt = null;
        waited = TimeSpan.Zero;
    }

    /// <summary>
    /// Null when nobody is being waited for. The seconds left are worked
    /// out now rather than when the wait began, so whoever is told has the
    /// same deadline as everybody else however late they arrived.
    /// </summary>
    private Waiting? DescribeWait()
    {
        if (hand is null || dueAt is not { } due || hand.WaitingSeat == -1)
        {
            return null;
        }

        var left = due - clock.GetUtcNow();

        return new Waiting(
            hand.WaitingSeat,
            Math.Max(left.TotalSeconds, 0),
            waited.TotalSeconds);
    }

    /// <summary>
    /// One player's own cards, which is the one thing here that never goes
    /// into <see cref="DescribeHand"/>.
    /// </summary>
    public IReadOnlyList<string> CardsOf(Guid playerId)
    {
        var seat = SeatOf(playerId);

        return hand is null || seat == -1 ? [] : [.. hand.Hands[seat].Select(card => card.Code)];
    }

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
                markJokers ? hand.JokerPositions : [],
                [.. hand.RevealedAtCut.Select(card => card.Code)],
                hand.FinishingCard?.Code,
                [.. hand.Hands.Select(cards => cards.Count)],
                hand.Passes,
                hand.Stock.Count,
                DescribeWait());

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, Jokers, [.. seats.Select(Occupant)], Started);

    private static TableSeat Occupant(PlayerSummary? player) =>
        player is null
            ? new TableSeat(null, null, null)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl);
}
