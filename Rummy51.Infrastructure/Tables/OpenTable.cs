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
///
/// A turn is given one clock and everything done inside it runs on that one
/// clock - drawing, laying, taking back, buying. Only a throw restarts it,
/// because only a throw hands the turn on. The moves below say nothing about
/// the clock for that reason; where one does, it is the exception.
/// </summary>
internal sealed class OpenTable
{
    private readonly PlayerSummary?[] seats;
    private readonly TestingSettings testing;
    private readonly TurnSettings turns;
    private readonly Countdown countdown;
    private Hand? hand;
    private bool dealtOnce;

    public OpenTable(
        CreateTableRequest request,
        TestingSettings testing,
        TurnSettings turns,
        TimeProvider clock)
    {
        this.testing = testing;
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
        if (hand is null)
        {
            return false;
        }

        if (testing.FourPairs && !dealtOnce)
        {
            hand = TestingDeals.ShuffledUntilFourPairs(hand, seats.Length, Jokers, position);
        }

        dealtOnce = true;

        if (!hand.CutAt(position))
        {
            return false;
        }

        hand.Deal();

        // Not a wait for anybody: the cards are going out, and the screen
        // takes a few seconds over what the server did at once.
        countdown.Restart(TimeSpan.FromSeconds(turns.DealSeconds));

        return true;
    }

    /// <summary>Rules 3.2, 7.1.</summary>
    public bool Draw(int seat, DrawnFrom from) => hand?.Draw(seat, from) == true;

    /// <summary>Rules 4.1, 5.6.</summary>
    public bool LayDown(int seat, IReadOnlyList<int> cardIds) =>
        hand?.LayDown(seat, cardIds) == true;

    /// <summary>Rules 3.6, 7.4.</summary>
    public bool Undo(int seat) => hand?.Undo(seat) == true;

    /// <summary>Rule 5.6, which is one of the reasons a throw is refused.</summary>
    public bool CouldEndTheTurn(int seat) => hand?.CanEndTheTurn(seat) != false;

    public bool HasPutBackWhatItTook(int seat) => hand?.HasPutBackWhatItTook(seat) != false;

    public bool MustStillGoOut(int seat) => hand?.MustStillGoOut(seat) == true;

    public bool CouldPartWith(int seat, int many) => hand?.CanPartWith(seat, many) != false;

    public bool GoingOutIsOpen => hand?.CanFinish != false;

    /// <summary>Rules 5.7, 5.9, 5.10.</summary>
    public bool AddTo(int seat, int meldAt, int cardId, MeldEnd end) =>
        hand?.AddTo(seat, meldAt, cardId, end) == true;

    /// <summary>Rules 5.11, 5.12.</summary>
    public bool SwapForJoker(int seat, int meldAt, int at, int cardId) =>
        hand?.SwapForJoker(seat, meldAt, at, cardId) == true;

    /// <summary>Rules 12.1, 12.2.</summary>
    public bool CanOfferARedeal(int seat) => hand?.CanOfferARedeal(seat) == true;

    /// <summary>Rule 12.1. The turn's clock gives way to the offer's.</summary>
    public bool OfferARedeal(int seat) => Moved(() => hand?.OfferARedeal(seat) == true);

    /// <summary>Rules 12.9 and 12.10, which do not wait for a turn.</summary>
    public bool ForceARedeal(int seat, bool atOnce) =>
        Moved(() => hand?.ForceARedeal(seat, atOnce) == true);

    /// <summary>Rule 12.3.</summary>
    public bool Answer(int seat, bool agreed) => Moved(() => hand?.Answer(seat, agreed) == true);

    /// <summary>Rule 12.4.</summary>
    public bool ReadTheResult(int seat) => Moved(() => hand?.Read(seat) == true);

    /// <summary>Rule 12.6.</summary>
    public bool Keep(int seat, IReadOnlyList<int> cardIds) =>
        Moved(() => hand?.Keep(seat, cardIds) == true);

    /// <summary>
    /// A move of a redeal, which is the one kind that moves the hand from
    /// one stage to another - so unlike the moves inside a turn, each one
    /// hands the clock to whatever the hand has become.
    /// </summary>
    private bool Moved(Func<bool> move)
    {
        var was = hand?.Phase;

        if (!move())
        {
            return false;
        }

        // Only where the stage itself changed. An answer among three is a
        // move inside the asking, and a table that began its twenty seconds
        // again at every one of them would never run out of them.
        if (hand?.Phase != was)
        {
            WaitForTheHand();
        }

        return true;
    }

    private void WaitForTheHand() =>
        countdown.Restart(TimeSpan.FromSeconds(hand?.Phase switch
        {
            HandPhase.Cutting => turns.CutSeconds,
            HandPhase.Dealing => turns.DealSeconds,
            HandPhase.Offering => turns.OfferSeconds,
            HandPhase.Answered => turns.ResultSeconds,
            HandPhase.Keeping => turns.KeepSeconds,
            _ => turns.TurnSeconds,
        }));

    /// <summary>Rule 3.2, which hands the turn on and the clock with it.</summary>
    public bool Discard(int seat, int cardId)
    {
        if (hand?.Discard(seat, cardId) != true)
        {
            return false;
        }

        countdown.Restart(TimeSpan.FromSeconds(turns.TurnSeconds));

        return true;
    }

    /// <summary>Rule 3.6, so one player who walked away cannot hold the rest.</summary>
    public bool ActForWhoeverRanOut()
    {
        if (hand is null || !countdown.HasRunOut)
        {
            return false;
        }

        if (hand.Phase == HandPhase.Cutting)
        {
            // There is no wrong place to cut, so it is cut where chance says.
            return CutAt(Random.Shared.Next(hand.Deck.Size));
        }

        if (hand.Phase == HandPhase.Dealing)
        {
            return BeginPlay();
        }

        if (hand.Phase is HandPhase.Offering or HandPhase.Answered or HandPhase.Keeping)
        {
            return Moved(() => hand.Phase switch
            {
                HandPhase.Offering => hand.TakeSilenceAsAgreement(),
                HandPhase.Answered => hand.StopShowingTheResult(),
                _ => hand.KeepForWhoeverHasNot(),
            });
        }

        PlayTheTurnFor(hand.TurnSeat);

        // However that went, the table is no longer waiting for the move it
        // was waiting for. A wait left standing is asked again every tick,
        // for ever.
        countdown.Restart(TimeSpan.FromSeconds(turns.TurnSeconds));

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

    /// <summary>Rule 3.6. What it means for the cards is the hand's to say.</summary>
    private bool PlayTheTurnFor(int seat) => hand?.PlayOutTheTurn(seat) == true;

    /// <summary>Rule 11.1: the one thing here that never goes into a <see cref="HandView"/>.</summary>
    public IReadOnlyList<HeldCardView> CardsOf(Guid playerId)
    {
        var seat = SeatOf(playerId);

        if (hand is null || seat == -1)
        {
            return [];
        }

        var owed = hand.StillOwed(seat).ToHashSet();

        return [.. hand.Hands[seat].Select(card => Held(card, owed.Contains(card.Id)))];
    }

    /// <summary>Rules 12.4 and 12.9: the half of a redeal that is one seat's own.</summary>
    public RedealView RedealAsSeenBy(Guid playerId)
    {
        var seat = SeatOf(playerId);

        return new RedealView(
            hand?.OfferAsSeenBy(seat) ?? [],
            hand?.CanForceARedeal(seat) == true ? hand.FourPairsOf(seat) : []);
    }

    /// <summary>Rule 11.1: worked out from a hand only one player may see.</summary>
    private HeldCardView Held(HeldCard card, bool owed) =>
        new(
            card.Id,
            card.Card.Code,
            [.. WhereItFits(card.Card)],
            [.. WhatItBuys(card.Card)],
            owed);

    private IEnumerable<FitView> WhereItFits(Card card) =>
        Lying().SelectMany((meld, at) =>
            Melding.WhereItFits(meld, card).Select(end => new FitView(at, end)));

    private IEnumerable<SwapView> WhatItBuys(Card card) =>
        Lying().SelectMany((meld, at) => Enumerable
            .Range(0, meld.Count)
            .Where(place => Melding.CanReplace(meld, place, card))
            .Select(place => new SwapView(at, place)));

    /// <summary>As the rules read it: cards and no more.</summary>
    private IEnumerable<IReadOnlyList<Card>> Lying() =>
        (hand?.Board.Melds ?? []).Select(IReadOnlyList<Card> (meld) =>
            [.. meld.Cards.Select(lying => lying.Card)]);

    public HandView? DescribeHand() =>
        hand is null
            ? null
            : new HandView(
                hand.Phase,
                hand.DealerSeat,
                hand.CutterSeat,
                hand.Deck.Size,
                hand.Deck.CutPosition,
                [.. hand.Deck.Backs.Select(back => $"{back}B")],
                testing.MarkJokers ? hand.Deck.JokerPositions : [],
                [.. hand.Deck.RevealedAtCut.Select(card => card.Code)],
                hand.Deck.FinishingCard?.Code,
                [.. hand.Hands.Select(cards => cards.Count)],
                hand.Passes,
                hand.Board.Stock.Count,
                hand.TurnSeat,
                hand.WinnerSeat,
                hand.WentDown,
                hand.HasDrawn,
                hand.Board.TopOfDiscards?.Code,
                hand.Board.Discards.Count,
                [.. hand.Board.Melds.Select(Laid)],
                hand.CanUndo,
                hand.TurnSeat != -1 && hand.CanAddToTheTable(hand.TurnSeat),
                hand.TurnSeat != -1 && hand.CanTakeTheFinishingCard(hand.TurnSeat),
                hand.TurnSeat != -1 && hand.CanOfferARedeal(hand.TurnSeat),
                hand.OfferedBy,
                hand.Forced,
                hand.ForcedWith,
                countdown.Describe(hand.WaitingSeat));

    /// <summary>Rule 11.3.</summary>
    private static MeldView Laid(Meld meld) =>
        new(meld.Seat, [.. meld.Cards.Select(card => new LaidCardView(card.Id, card.Card.Code))]);

    public LobbyTable Describe() =>
        new(Id, Mode, Limit, Jokers, [.. seats.Select(Occupant)], Started);

    private static TableSeat Occupant(PlayerSummary? player) =>
        player is null
            ? new TableSeat(null, null, null)
            : new TableSeat(player.PlayerId, player.DisplayName, player.AvatarUrl);
}
