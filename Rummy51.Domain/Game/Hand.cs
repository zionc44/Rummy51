namespace Rummy51.Domain.Game;

public enum HandPhase
{
    Cutting,

    Dealing,

    Playing,

    Finished
}

public enum DrawnFrom
{
    Stock,

    /// <summary>
    /// The card the cut stopped on. It is not a pile and nobody draws from
    /// it to play on: taking it is a promise to go out this turn.
    /// </summary>
    Finishing,

    Discards
}

/// <summary>
/// One hand, from the shuffle to its end, and the rules of it enforced.
/// What those rules are is written in docs/rules.md; what is here is how
/// they are held to, and what they deliberately do not yet hold anyone to.
///
/// It knows nothing of the table's mode: a single hand, one of a survival
/// run and the third of a series are the same hand played the same way, and
/// what follows it is somebody else's question.
/// </summary>
public sealed class Hand
{
    private readonly List<List<HeldCard>> hands;
    private readonly TurnSoFar turn = new();
    private readonly bool[] wentDown;

    /// <summary>
    /// Turns that have ended, which is how the first round is counted. A
    /// turn that was taken back and taken again is still one turn: what is
    /// counted is seats that have had their go, not moves made.
    /// </summary>
    private int turnsTaken;
    private int given;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
        hands = [.. Enumerable.Range(0, seats).Select(_ => new List<HeldCard>())];
        wentDown = new bool[seats];
        DealerSeat = dealerSeat;
        CutterSeat = (dealerSeat + seats - 1) % seats;
        Deck = new Deck(jokers, seed);
    }

    public int Seats { get; }
    public int DealerSeat { get; }
    public int CutterSeat { get; }

    /// <summary>
    /// The cards this hand is played from, and what the cut turned up in
    /// them. A hand says when the deck may be cut; what is in it is the
    /// deck's own.
    /// </summary>
    public Deck Deck { get; }

    public HandPhase Phase { get; private set; } = HandPhase.Cutting;

    /// <summary>Whose turn it is, or -1 before play has begun.</summary>
    public int TurnSeat { get; private set; } = -1;

    /// <summary>Who took the hand, or -1 while it is still being played.</summary>
    public int WinnerSeat { get; private set; } = -1;

    public bool HasDrawn { get; private set; }

    /// <summary>
    /// Whose move the hand is waiting for, or -1 when it is waiting for
    /// nobody - the deal included, where the cards are going out and there
    /// is nobody to hurry. How long it waits is the table's business; who it
    /// waits for is a rule of the game and is here.
    /// </summary>
    public int WaitingSeat => Phase switch
    {
        HandPhase.Cutting => CutterSeat,
        HandPhase.Playing => TurnSeat,
        _ => -1,
    };

    /// <summary>
    /// What each seat is holding, in the order it arrived. The one thing a
    /// hand knows that belongs to one player rather than to the table.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<HeldCard>> Hands => hands;

    /// <summary>
    /// Everything lying on the felt. A hand says who may reach for it and
    /// when; what is there is the board's own.
    /// </summary>
    public Board Board { get; } = new();

    /// <summary>Public: their melds are lying there for everybody to read.</summary>
    public IReadOnlyList<bool> WentDown => wentDown;

    /// <summary>What that hand would cost its owner if the hand ended now.</summary>
    public int WorthOf(int seat) =>
        wentDown[seat] ? Worth.Held(hands[seat].Select(card => card.Card)) : Worth.NeverWentDown;

    /// <summary>
    /// Whether this seat could end its turn on what it has laid. Laying
    /// nothing is always allowed - nobody has to go down - and a player who
    /// is down already may lay whatever is a meld. What makes a first
    /// showing stand is a rule, and lives with the rules.
    /// </summary>
    public bool CanEndTheTurn(int seat) =>
        HasPutBackWhatItTook(seat)
        && !MustStillGoOut(seat)
        && (wentDown[seat] || turn.Laying.Count == 0 || Melding.IsAShowing(Showing));

    /// <summary>
    /// Whether anybody may go out yet. Nobody goes out before every other
    /// seat has had a turn: a hand that ended sooner would be lost by
    /// players who were never given the chance to go down. The seat
    /// completing the round is not one of them - by its turn everybody else
    /// has had theirs - so the last turn of the first round can win it.
    /// </summary>
    public bool CanFinish => turnsTaken >= Seats - 1;

    /// <summary>
    /// Whether this seat may take the finishing card. It is for going out
    /// in one strike and for nothing else, so a player who has already had
    /// their hand on the table cannot have it - and nobody can while going
    /// out is still closed, because taking it is going out.
    /// </summary>
    public bool CanTakeTheFinishingCard(int seat) =>
        IsTheirTurn(seat)
        && !HasDrawn
        && !wentDown[seat]
        && CanFinish
        && Deck.FinishingCard is not null;

    /// <summary>
    /// Whether this turn took the finishing card, and so has promised the
    /// table to end the hand before it ends the turn.
    /// </summary>
    private bool PromisedToGoOut => turn.TheFinishingCard is not null;

    /// <summary>
    /// Whether that promise is still owed. The only throw that keeps it is
    /// the last card in the hand, so a turn that took the card and did not
    /// lay the rest of itself down cannot end at all - it is taken back
    /// instead, and the card goes back where it was lying.
    /// </summary>
    public bool MustStillGoOut(int seat) => PromisedToGoOut && hands[seat].Count != 1;

    /// <summary>
    /// How many cards a hand keeps back from the table. One, because a turn
    /// ends on a throw and on nothing else: a hand laid out to the last card
    /// could not end its own turn.
    ///
    /// Two while nobody may go out, which is what holds the first round
    /// open. A hand down to one card has no move left but to throw it and
    /// go out, so the way to forbid that is never to let the hand get
    /// there - and a player is stopped while they still have a turn to
    /// play rather than stranded holding a card they may not throw.
    /// </summary>
    private int KeepsBack => CanFinish ? 1 : 2;

    /// <summary>
    /// Whether this seat can put that many cards on the table and still
    /// have a hand to play.
    ///
    /// Which is what makes going out a throw rather than a lay. Three cards
    /// left that happen to be a run are not a win - two of them are no
    /// meld, so the run waits a turn.
    /// </summary>
    private bool CanPartWith(int seat, int many) => hands[seat].Count - many >= KeepsBack;

    /// <summary>
    /// Whether everything this turn took off the table is back on it. The
    /// card off the discard pile and a joker bought out of a meld are the
    /// same rule and not two: both were taken from the table, and whatever
    /// is taken from the table is on it again before the turn ends.
    ///
    /// A card still owed is a card still in the hand, so the question is
    /// asked of the hand. The card about to be thrown is in it when this is
    /// asked, which is what stops a player throwing back the very card they
    /// took - they took it to use it.
    /// </summary>
    public bool HasPutBackWhatItTook(int seat) => StillOwed(seat).Count == 0;

    /// <summary>
    /// Which cards those are, so that the player can be shown the one they
    /// have to put down rather than told that one exists. Empty for a seat
    /// that is not playing: a turn's debts are its own.
    /// </summary>
    public IReadOnlyList<int> StillOwed(int seat) =>
        IsTheirTurn(seat)
            ? [.. turn.Owed.Where(owed => hands[seat].Any(card => card.Id == owed))]
            : [];

    /// <summary>
    /// Whether this seat may add to what is lying on the table, or buy a
    /// joker out of it. Going down is what earns the right, and it is
    /// earned the moment what has been laid this turn is a showing - not
    /// when the throw makes it final. Those are the same turn, and the
    /// written rule says doing both in one is fine.
    ///
    /// Which means it can be lost again in the same turn: take back part of
    /// a showing and it stops being one, and the right goes with it. That
    /// is the same breath in which the right to end the turn goes, so a
    /// player cannot be left having added to a table they never joined.
    /// </summary>
    public bool CanAddToTheTable(int seat) =>
        !PromisedToGoOut
        && (wentDown[seat] || (turn.Laying.Count > 0 && Melding.IsAShowing(Showing)));

    /// <summary>What this turn has laid, as the rules of going down read it.</summary>
    private IReadOnlyList<IReadOnlyList<Card>> Showing => [.. turn.Laying.Select(Lying)];

    /// <summary>The deal as the eye should see it, pass by pass.</summary>
    public IReadOnlyList<DealPass> Passes { get; private set; } = [];

    /// <summary>
    /// False when the hand is past this point, which is what a second click
    /// on a deck already cut looks like from here.
    /// </summary>
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
    /// Play begins. The opener is marked as having drawn already, because
    /// their fifteenth card was the draw: they start their turn halfway
    /// through it, with nothing to do but discard.
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

    /// <summary>Deals what the cut left. False when it has been dealt already.</summary>
    public bool Deal()
    {
        if (Phase != HandPhase.Dealing || Passes.Count > 0)
        {
            return false;
        }

        var dealt = Dealing.Deal(Deck.Remaining, Seats, DealerSeat, Deck.RevealedAtCut.Count);

        for (var seat = 0; seat < Seats; seat++)
        {
            // A joker the cut gave the cutter is already theirs, and opens
            // the hand they are dealt the rest of.
            if (seat == CutterSeat)
            {
                Give(seat, Deck.RevealedAtCut);
            }

            Give(seat, dealt.Hands[seat]);
        }

        Board.StockWith(dealt.Stock);
        Passes = dealt.Passes;

        return true;
    }

    /// <summary>
    /// False when it is not that seat's turn, when they have taken a card
    /// already, or when there is nothing where they reached.
    /// </summary>
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
    /// Ends the turn. The card is named by the number it was given rather
    /// than by what it is: two sevens of hearts are the same card, and a
    /// player throwing one of them means that one.
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

        // The throw is what makes a showing final. Until it lands, what was
        // laid can still be taken back and nobody has gone down on it.
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
    /// The hand is over and this seat took it: they laid every card they
    /// held but one and threw that one. Nobody is waited for afterwards -
    /// <see cref="WaitingSeat"/> answers for no seat once the hand is not
    /// being played, so the clock stops without being told to.
    /// </summary>
    private void Won(int seat)
    {
        WinnerSeat = seat;
        Phase = HandPhase.Finished;
        TurnSeat = -1;
        HasDrawn = false;
        Forget();
    }

    /// <summary>
    /// A turn nobody took, played out for them. How long a table waits
    /// before deciding a turn is nobody's is the table's own business; what
    /// happens then is a rule of the game, so it is here.
    /// </summary>
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

        // Nothing left to throw, which a hand being played should never
        // reach. The turn goes on anyway: a table that cannot end a turn is
        // a table nobody can leave.
        if (hands[seat].Count == 0)
        {
            HandItOn();

            return true;
        }

        return Discard(seat, WhatCameOffTheStock(seat));
    }

    /// <summary>
    /// Whoever opens the hand drew theirs during the deal and has no such
    /// card, so for them it is the last one they were dealt.
    /// </summary>
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
    /// Cards onto the table, which happens between the two halves of a turn
    /// and ends neither. What they come to is not asked here: the fifty-one
    /// is asked of a whole turn, when the turn ends.
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

        var meld = new Meld(seat, [.. places.Select(at => held[at])]);

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

    /// <summary>
    /// A card onto a meld already lying there, which only a player who has
    /// gone down may do.
    /// </summary>
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

        Board.Grow(meldAt, end, card);
        hands[seat].RemoveAt(held);
        turn.Joined(meldAt, card);

        return true;
    }

    /// <summary>
    /// The joker comes into the hand keeping the number it has had since it
    /// was dealt: a card is given its number once and carries it wherever it
    /// goes, which is what lets the screen follow one across the table.
    ///
    /// Only a player who has gone down may buy one, for the same reason
    /// they alone may add to a meld, and it is owed back to the table
    /// before the turn ends like anything else taken off it. A hand keeps
    /// the size it had: a card goes out and the joker comes in, so this is
    /// the one way onto the table that cannot empty a hand.
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

        var joker = Board.Swap(meldAt, at, card);

        hands[seat].RemoveAt(held);
        hands[seat].Add(joker);
        turn.Swapped(meldAt, at, card, joker);

        return true;
    }

    /// <summary>
    /// Everybody is told: a meld laid and then taken up again is something
    /// the whole table watched happen.
    /// </summary>
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
    /// The draw undone, to whichever of the two places allow it: the top of
    /// the discard pile, or the finishing card's place beside the deck.
    /// Both were face up, so nobody learns anything from a card that went
    /// and came back. The stock is the one that cannot.
    ///
    /// Found by the number it was given when it was taken, so the one that
    /// goes back is the one that came - and not its twin out of the other
    /// pack, which the player may have been holding all along.
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
