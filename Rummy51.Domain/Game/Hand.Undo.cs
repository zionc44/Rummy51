namespace Rummy51.Domain.Game;

/// <summary>
/// Anything a turn did, taken back, last thing first. It is its own file
/// rather than part of the turn because undoing is not the move run
/// backwards: a card off the stock never goes back, since putting it back
/// would let a player look at the top of the stock and then decide not to
/// have looked.
/// </summary>
public sealed partial class Hand
{
    /// <summary>Public: the whole table watched it happen.</summary>
    public bool CanUndo => turn.Anything;

    /// <summary>
    /// The melds come back before the drawn card, because one of them may
    /// hold the very card that was taken off the pile.
    /// </summary>
    public bool Undo(int seat)
    {
        if (!IsTheirTurn(seat))
        {
            return false;
        }

        var anything = false;

        while (TakeBackTheLastThing(seat) || PutTheDrawnCardBack(seat))
        {
            anything = true;
        }

        return anything;
    }

    private bool TakeBackTheLastThing(int seat)
    {
        var last = turn.TakeBackTheLast();

        if (last is null)
        {
            return false;
        }

        if (last is Move.Joined(var at, var card))
        {
            Board.Shrink(at, card.Id);
            hands[seat].Add(card);

            return true;
        }

        if (last is Move.Swapped(var meldAt, var place, var paid, var joker))
        {
            Board.Swap(meldAt, place, joker);
            hands[seat].RemoveAll(held => held.Id == joker.Id);
            hands[seat].Add(paid);

            return true;
        }

        if (Board.TakeBackTheLast() is { } meld)
        {
            hands[meld.Seat].AddRange(meld.Cards);
        }

        return true;
    }

    /// <summary>
    /// Rules 3.6 and 7.4, to whichever of the two places allow it. Found by
    /// the number it was given, so the card that goes back is the one that
    /// came and not its twin out of the other pack.
    /// </summary>
    private bool PutTheDrawnCardBack(int seat)
    {
        var thrown = turn.TheDiscard;

        if ((thrown ?? turn.TheFinishingCard) is not { } number)
        {
            return false;
        }

        var at = hands[seat].FindIndex(card => card.Id == number);

        if (at == -1)
        {
            return false;
        }

        if (thrown is not null)
        {
            Board.Throw(hands[seat][at].Card);
        }
        else
        {
            Deck.GiveTheFinishingCardBack();
        }

        hands[seat].RemoveAt(at);
        HasDrawn = false;
        turn.GaveItBack();

        return true;
    }
}
