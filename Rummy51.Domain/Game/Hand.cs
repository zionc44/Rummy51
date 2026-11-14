namespace Rummy51.Domain.Game;

public enum HandPhase
{
    Cutting,

    Dealing,

    Playing,

    Offering,

    Answered,

    Keeping,

    Finished
}

public enum DrawnFrom
{
    Stock,

    /// <summary>Rule 7.1.</summary>
    Finishing,

    Discards
}

/// <summary>
/// One hand, from the shuffle to its end, and the rules of it enforced. What
/// those rules are is docs/rules.md; what is here is how they are held to.
///
/// It knows nothing of the table's mode (rule 9.2): a single hand, one of a
/// survival run and the third of a series are the same hand played the same
/// way, and what follows it is somebody else's question.
///
/// Chapter 12 is in Hand.Redeal.cs, where the fields it alone uses are
/// declared as well. A hand being dealt again is a stage of a hand and not
/// a thing of its own, which is why it is the same class and not another.
/// </summary>
public sealed partial class Hand
{
    private readonly List<List<HeldCard>> hands;
    private readonly TurnSoFar turn = new();
    private readonly bool[] wentDown;

    /// <summary>
    /// How the first round is counted. A turn taken back and taken again is
    /// still one turn: seats that have had their go, not moves made.
    /// </summary>
    private int turnsTaken;
    private int given;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
        hands = [.. Enumerable.Range(0, seats).Select(_ => new List<HeldCard>())];
        wentDown = new bool[seats];
        hasOffered = new bool[seats];
        keeping = new int[seats][];
        DealerSeat = dealerSeat;
        CutterSeat = (dealerSeat + seats - 1) % seats;
        Deck = new Deck(jokers, seed);
    }

    public int Seats { get; }
    public int DealerSeat { get; }
    public int CutterSeat { get; }

    /// <summary>A hand says when it may be cut; what is in it is its own.</summary>
    public Deck Deck { get; private set; }

    public HandPhase Phase { get; private set; } = HandPhase.Cutting;

    /// <summary>Whose turn it is, or -1 before play has begun.</summary>
    public int TurnSeat { get; private set; } = -1;

    /// <summary>Who took the hand, or -1 while it is still being played.</summary>
    public int WinnerSeat { get; private set; } = -1;

    public bool HasDrawn { get; private set; }

    /// <summary>
    /// Or -1 when it waits for nobody, the deal included. How long it waits
    /// is the table's business under rule 3.6; who it waits for is here.
    /// </summary>
    public int WaitingSeat => Phase switch
    {
        HandPhase.Cutting => CutterSeat,
        HandPhase.Playing => TurnSeat,
        _ => -1,
    };

    /// <summary>The one thing here that belongs to a player and not the table.</summary>
    public IReadOnlyList<IReadOnlyList<HeldCard>> Hands => hands;

    /// <summary>A hand says who may reach for it; what is there is its own.</summary>
    public Board Board { get; private set; } = new();

    /// <summary>Public: their melds are lying there for everybody to read.</summary>
    public IReadOnlyList<bool> WentDown => wentDown;

    /// <summary>Rule 8.1.</summary>
    public int WorthOf(int seat) =>
        wentDown[seat] ? Worth.Held(hands[seat].Select(card => card.Card)) : Worth.NeverWentDown;

    /// <summary>Rules 3.4, 5.6, 7.2. Laying nothing is always allowed.</summary>
    public bool CanEndTheTurn(int seat) =>
        HasPutBackWhatItTook(seat)
        && !MustStillGoOut(seat)
        && (wentDown[seat] || turn.Laying.Count == 0 || Melding.IsAShowing(Showing));

    /// <summary>Rule 6.4.</summary>
    public bool CanFinish => turnsTaken >= Seats - 1;

    /// <summary>Rules 7.1, 7.3, and 6.4 because taking it is going out.</summary>
    public bool CanTakeTheFinishingCard(int seat) =>
        IsTheirTurn(seat)
        && !HasDrawn
        && !wentDown[seat]
        && CanFinish
        && Deck.FinishingCard is not null;

    /// <summary>Rule 7.1: taking it promised the table a win this turn.</summary>
    private bool PromisedToGoOut => turn.TheFinishingCard is not null;

    /// <summary>Rule 7.4: a turn that cannot keep the promise ends no other way.</summary>
    public bool MustStillGoOut(int seat) => PromisedToGoOut && hands[seat].Count != 1;

    /// <summary>
    /// Rules 6.2 and 6.5. Two rather than one is how 6.4 is held: a hand that
    /// never reaches its last card can never be made to throw it, so nobody
    /// is stranded holding a card they are forbidden to throw.
    /// </summary>
    private int KeepsBack => CanFinish ? 1 : 2;

    /// <summary>Rule 6.2, which is what makes going out a throw and not a lay.</summary>
    public bool CanPartWith(int seat, int many) => hands[seat].Count - many >= KeepsBack;

    /// <summary>
    /// Rule 3.4. The card about to be thrown is still in the hand when this
    /// is asked, which is what stops a player throwing back the very card
    /// they took.
    /// </summary>
    public bool HasPutBackWhatItTook(int seat) => StillOwed(seat).Count == 0;

    /// <summary>
    /// So the player can be shown the card rather than told one exists.
    /// Empty for a seat not playing: a turn's debts are its own.
    /// </summary>
    public IReadOnlyList<int> StillOwed(int seat) =>
        IsTheirTurn(seat)
            ? [.. turn.Owed.Where(owed => hands[seat].Any(card => card.Id == owed))]
            : [];

    /// <summary>Rules 5.7, 5.8, 5.12, and 7.2 while the finishing card is held.</summary>
    public bool CanAddToTheTable(int seat) =>
        !PromisedToGoOut
        && (wentDown[seat] || (turn.Laying.Count > 0 && Melding.IsAShowing(Showing)));

    /// <summary>Rule 5.6: one turn is one move.</summary>
    private IReadOnlyList<IReadOnlyList<Card>> Showing => [.. turn.Laying.Select(Lying)];

    /// <summary>Pass by pass, so the eye can follow it. Rule 2.4.</summary>
    public IReadOnlyList<DealPass> Passes { get; private set; } = [];

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
            dealingAgain ? Dealing.Redeal : Dealing.FirstDeal);

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
        dealingAgain = false;

        return true;
    }

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

        // Rule 5.8: the throw is what makes a showing final.
        wentDown[seat] = wentDown[seat] || turn.Laying.Count > 0;

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
        turnsTaken += 1;
        TurnSeat = (TurnSeat + 1) % Seats;
        HasDrawn = false;
        Forget();
    }

    /// <summary>
    /// Rules 4.1 and 6.2. What it comes to is not asked here: rule 5.6 asks
    /// that of a whole turn, when the turn ends.
    /// </summary>
    public bool LayDown(int seat, IReadOnlyList<int> cardIds)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || cardIds.Count == 0)
        {
            return false;
        }

        // One card named twice would be laid twice and taken out once.
        if (cardIds.Distinct().Count() != cardIds.Count)
        {
            return false;
        }

        var held = hands[seat];
        var places = cardIds.Select(id => held.FindIndex(card => card.Id == id)).ToList();

        if (places.Contains(-1))
        {
            return false;
        }

        if (!CanPartWith(seat, places.Count))
        {
            return false;
        }

        var meld = new Meld(seat, [.. places.Select(at => Owned(held[at], seat))]);

        if (!Melding.IsMeld([.. meld.Cards.Select(lying => lying.Card)]))
        {
            return false;
        }

        Board.Lay(meld);
        turn.Laid(Board.Melds.Count - 1);

        // From the back, so that taking one out does not move the places of
        // those still to be taken.
        foreach (var at in places.OrderDescending())
        {
            held.RemoveAt(at);
        }

        return true;
    }

    /// <summary>Rules 5.7, 5.9, 5.10, 6.2.</summary>
    public bool AddTo(int seat, int meldAt, int cardId, MeldEnd end)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || !CanAddToTheTable(seat))
        {
            return false;
        }

        if (meldAt < 0 || meldAt >= Board.Melds.Count)
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1 || !CanPartWith(seat, 1))
        {
            return false;
        }

        var card = hands[seat][held];
        var meld = Board.Melds[meldAt];

        if (!Melding.WhereItFits([.. meld.Cards.Select(lying => lying.Card)], card.Card).Contains(end))
        {
            return false;
        }

        Board.Grow(meldAt, end, Owned(card, seat));
        hands[seat].RemoveAt(held);
        turn.Joined(meldAt, card);

        return true;
    }

    /// <summary>
    /// Rules 5.11, 5.12, 6.3. The joker comes into the hand keeping the
    /// number it has had since it was dealt, which is what lets the screen
    /// follow one across the table.
    /// </summary>
    public bool SwapForJoker(int seat, int meldAt, int at, int cardId)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || !CanAddToTheTable(seat))
        {
            return false;
        }

        if (meldAt < 0 || meldAt >= Board.Melds.Count)
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1)
        {
            return false;
        }

        var card = hands[seat][held];
        var meld = Board.Melds[meldAt];

        if (!Melding.CanReplace([.. meld.Cards.Select(lying => lying.Card)], at, card.Card))
        {
            return false;
        }

        var joker = Board.Swap(meldAt, at, Owned(card, seat));

        hands[seat].RemoveAt(held);
        hands[seat].Add(joker);
        turn.Swapped(meldAt, at, card, joker);

        return true;
    }

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

    private void Forget() => turn.Forget();

    private bool IsTheirTurn(int seat) => Phase == HandPhase.Playing && seat == TurnSeat;

    /// <summary>
    /// Whose a card is once it is on the table. Rule 12.5 fixes it the first
    /// time it is laid and not again, so a joker bought out of a meld and
    /// laid somewhere else is still the card of whoever first put it there.
    ///
    /// Nothing ever has to put it back to nobody. A card thrown away loses
    /// it anyway - the pile holds cards and not held cards - and the only
    /// card that reaches a hand already owned is a joker bought out of a
    /// meld, which rule 3.4 will not let its buyer keep.
    /// </summary>
    private static HeldCard Owned(HeldCard card, int seat) =>
        card.Owner == HeldCard.Nobody ? card with { Owner = seat } : card;

    private IReadOnlyList<Card> Lying(int at) =>
        [.. Board.Melds[at].Cards.Select(card => card.Card)];

    /// <summary>
    /// Each card numbered as it arrives. That number is what the player's
    /// own arrangement is held by, so it has to outlive every sort and every
    /// move and is never given out twice.
    /// </summary>
    private void Give(int seat, IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            hands[seat].Add(new HeldCard(given++, card));
        }
    }
}
