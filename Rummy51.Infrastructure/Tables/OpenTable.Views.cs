using Rummy51.Application.Tables;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// The table as it is described to whoever is looking at it, which is two
/// answers and not one: chapter 11 to the whole table, and one player's own
/// cards to that player. Nothing here decides anything - it reads a hand
/// that has already moved, and the split is so that the moving and the
/// telling cannot be confused for each other.
/// </summary>
internal sealed partial class OpenTable
{
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
                hand.Scores,
                hand.Places,
                Doubled(hand),
                // Copied, not handed over: these arrays belong to the game
                // and go on changing, and a view is what was true when it
                // was asked for.
                new Standing([.. Totals], [.. HandsPlayed], [.. LastHand], [.. OutOfIt], WonSeat),
                staying ?? [],
                Absent,
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

    /// <summary>Rule 8.2, with what met each of its two conditions.</summary>
    private static Doubling Doubled(Hand hand) =>
        new(
            hand.RedealMultiplier,
            hand.WinMultiplier,
            hand.ResultMultiplier,
            hand.WinnerSeat != -1 && hand.WentRummy[hand.WinnerSeat],
            hand.WinnerSeat != -1 && hand.TookTheFinishingCard[hand.WinnerSeat]);

    /// <summary>Rule 11.3.</summary>
    private static MeldView Laid(Meld meld) =>
        new(meld.Seat, [.. meld.Cards.Select(card => new LaidCardView(card.Id, card.Card.Code))]);
}
