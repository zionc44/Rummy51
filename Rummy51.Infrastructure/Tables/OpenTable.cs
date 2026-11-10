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
    private readonly Countdown countdown;
    private Hand? hand;

    public OpenTable(
        CreateTableRequest request,
        bool markJokers,
        TurnSettings turns,
        TimeProvider clock)
    {
        this.markJokers = markJokers;
        this.turns = turns;
        countdown = new Countdown(clock);

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
            countdown.Restart(TimeSpan.FromSeconds(turns.CutSeconds));
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
        countdown.Restart(TimeSpan.FromSeconds(turns.DealSeconds));

        return true;
    }

    /// <summary>
    /// A player takes their card. The wait is not restarted: a turn is given
    /// one clock, and reaching for a card is part of what it is for.
    /// </summary>
    public bool Draw(int seat, DrawnFrom from) => hand?.Draw(seat, from) == true;

    /// <summary>
    /// Cards onto the table. The turn is not over and the clock is not
    /// restarted: laying one down is something done inside a turn, and the
    /// time it takes is time the player chose to spend.
    /// </summary>
    public bool LayDown(int seat, IReadOnlyList<int> cardIds) =>
        hand?.LayDown(seat, cardIds) == true;

    /// <summary>
    /// The last thing taken back. The clock is not touched: a turn spent
    /// changing your mind is still your turn being spent.
    /// </summary>
    public bool Undo(int seat) => hand?.Undo(seat) == true;

    /// <summary>
    /// A player throws one away, which hands the turn on - and with it the
    /// clock, which begins again for whoever it reached.
    /// </summary>
    public bool Discard(int seat, int cardId)
    {
        if (hand?.Discard(seat, cardId) != true)
        {
            return false;
        }

        countdown.Restart(TimeSpan.FromSeconds(turns.TurnSeconds));

        return true;
    }

    /// <summary>
    /// What the table does when nobody does it for themselves, so that one
    /// player who has walked away cannot hold three others still.
    /// </summary>
    public bool ActForWhoeverRanOut()
    {
        if (hand is null || !countdown.HasRunOut)
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

        if (PlayTheTurnFor(hand.TurnSeat))
        {
            return true;
        }

        // Nothing could be played for them, which is not a thing that should
        // happen. The wait ends rather than being asked again every tick.
        countdown.Stop();

        return true;
    }

    private bool BeginPlay()
    {
        if (hand?.Begin() != true)
        {
            return false;
        }

        countdown.Restart(TimeSpan.FromSeconds(turns.FirstTurnSeconds));

        return true;
    }

    /// <summary>
    /// A turn nobody took, played in the way that changes that hand least: a
    /// card off the stock and the same card straight back. Whoever opens the
    /// hand has already drawn - their fifteenth card was the draw - so for
    /// them it is the last card they were dealt that goes instead.
    /// </summary>
    private bool PlayTheTurnFor(int seat)
    {
        if (hand is null)
        {
            return false;
        }

        if (!hand.HasDrawn)
        {
            hand.Draw(seat, DrawnFrom.Stock);
        }

        var held = hand.Hands[seat];

        return held.Count > 0 && Discard(seat, held[^1].Id);
    }

    /// <summary>
    /// One player's own cards, which is the one thing here that never goes
    /// into <see cref="DescribeHand"/>.
    /// </summary>
    public IReadOnlyList<HeldCardView> CardsOf(Guid playerId)
    {
        var seat = SeatOf(playerId);

        return hand is null || seat == -1
            ? []
            : [.. hand.Hands[seat].Select(card => new HeldCardView(card.Id, card.Card.Code))];
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
                hand.TurnSeat,
                hand.HasDrawn,
                hand.TopOfDiscards?.Code,
                hand.Discards.Count,
                [.. hand.Melds.Select(Laid)],
                hand.CanUndo,
                countdown.Describe(hand.WaitingSeat));

    /// <summary>Everything on the table is everybody's to read.</summary>
    private static MeldView Laid(Meld meld) =>
        new(meld.Seat, [.. meld.Cards.Select(card => new HeldCardView(card.Id, card.Card.Code))]);

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, Jokers, [.. seats.Select(Occupant)], Started);

    private static TableSeat Occupant(PlayerSummary? player) =>
        player is null
            ? new TableSeat(null, null, null)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl);
}
