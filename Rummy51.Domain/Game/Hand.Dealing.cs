namespace Rummy51.Domain.Game;

/// <summary>
/// The cut, the deal that follows it, and the moment play begins. Three
/// moves with no decisions between them - at a table the cards are already
/// going out while anybody is still thinking about them.
/// </summary>
public sealed partial class Hand
{
    /// <summary>False past this point, which a second click looks like.</summary>
    public bool CutAt(int position)
    {
        if (Phase != HandPhase.Cutting)
        {
            return false;
        }

        Deck.CutAt(position);
        Phase = HandPhase.Dealing;

        return true;
    }

    /// <summary>
    /// Rule 3.1: the opener is marked as having drawn, their fifteenth card
    /// having been the draw.
    /// </summary>
    public bool Begin()
    {
        if (Phase != HandPhase.Dealing || Passes.Count == 0)
        {
            return false;
        }

        Phase = HandPhase.Playing;
        TurnSeat = (DealerSeat + 1) % Seats;
        HasDrawn = true;
        downBeforeThisTurn = false;
        Forget();

        return true;
    }

    /// <summary>Rule 2.4, from what the cut left.</summary>
    public bool Deal()
    {
        if (Phase != HandPhase.Dealing || Passes.Count > 0)
        {
            return false;
        }

        var dealt = Dealing.Deal(
            Deck.Remaining,
            Seats,
            DealerSeat,
            Deck.RevealedAtCut.Count,
            nextDeal);

        for (var seat = 0; seat < Seats; seat++)
        {
            // Rule 2.3: the cutter's joker opens the hand they are dealt.
            if (seat == CutterSeat)
            {
                Give(seat, Deck.RevealedAtCut);
            }

            Give(seat, dealt.Hands[seat]);
        }

        Board.StockWith(dealt.Stock);
        Passes = dealt.Passes;
        nextDeal = Dealing.FirstDeal;

        return true;
    }

    /// <summary>Pass by pass, so the eye can follow it. Rule 2.4.</summary>
    public IReadOnlyList<DealPass> Passes { get; private set; } = [];
}
