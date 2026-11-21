namespace Rummy51.Domain.Game;

/// <summary>
/// A card in and a card out, and the turn handed on. What a turn may put
/// on the table between the two is Hand.Laying.cs, and what it may take
/// back is Hand.Undo.cs; this is the turn itself.
/// </summary>
public sealed partial class Hand
{
    /// <summary>Rules 3.2 and 7.1.</summary>
    public bool Draw(int seat, DrawnFrom from)
    {
        if (!IsTheirTurn(seat) || HasDrawn)
        {
            return false;
        }

        var taken = from switch
        {
            DrawnFrom.Stock => Board.TakeFromStock(),
            DrawnFrom.Discards => Board.TakeFromDiscards(),
            _ => CanTakeTheFinishingCard(seat) ? Deck.TakeTheFinishingCard() : null,
        };

        if (taken is null)
        {
            return false;
        }

        // Read before the card is given one, which is the number it will be
        // given, and the only way to find it again afterwards.
        var number = given;

        Give(seat, [taken]);
        HasDrawn = true;
        tookTheFinishingCard[seat] |= from == DrawnFrom.Finishing;
        turn.Took(number, from);

        return true;
    }

    /// <summary>
    /// Ends the turn. The card is named by its number and not by what it is:
    /// two sevens of hearts are the same card, and a player throwing one of
    /// them means that one.
    /// </summary>
    public bool Discard(int seat, int cardId)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || !CanEndTheTurn(seat))
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1)
        {
            return false;
        }

        // Rule 5.8: standing as a showing is what puts a player down, and
        // the throw is what settles it. Written here so that nothing after
        // this turn can take it away again.
        wasDown[seat] = IsDown(seat);

        Board.Throw(hands[seat][held].Card);
        hands[seat].RemoveAt(held);

        if (hands[seat].Count == 0)
        {
            Won(seat);

            return true;
        }

        HandItOn();

        return true;
    }

    /// <summary>
    /// Rule 6.1. Nobody is waited for afterwards - <see cref="WaitingSeat"/>
    /// answers for no seat once the hand is not being played, so the clock
    /// stops without being told to.
    /// </summary>
    private void Won(int seat)
    {
        // Rule 6.6: everything they laid went down in this one turn, and
        // none of it came off the table.
        wentRummy[seat] = !downBeforeThisTurn && !turn.LeanedOnTheTable;

        WinnerSeat = seat;
        Phase = HandPhase.Finished;
        TurnSeat = -1;
        HasDrawn = false;
        Forget();
    }

    /// <summary>Rule 3.6. How long a table waits first is its own business.</summary>
    public bool PlayOutTheTurn(int seat)
    {
        if (!IsTheirTurn(seat))
        {
            return false;
        }

        Undo(seat);

        if (!HasDrawn)
        {
            Draw(seat, DrawnFrom.Stock);
        }

        // Rule 6.2 says this cannot happen. The turn goes on anyway: a
        // table that cannot end a turn is a table nobody can leave.
        if (hands[seat].Count == 0)
        {
            HandItOn();

            return true;
        }

        return Discard(seat, WhatCameOffTheStock(seat));
    }

    /// <summary>Rule 3.1 leaves the opener none, so theirs is the last dealt.</summary>
    private int WhatCameOffTheStock(int seat) => turn.OffTheStock ?? hands[seat][^1].Id;

    /// <summary>The only way a turn ever ends.</summary>
    private void HandItOn()
    {
        hasPlayed[TurnSeat] = true;
        turnsTaken += 1;
        TurnSeat = Dealing.After(TurnSeat, playing);
        HasDrawn = false;
        downBeforeThisTurn = wasDown[TurnSeat];
        Forget();
    }
}
