namespace Rummy51.Domain.Game;

public enum HandPhase
{
    /// <summary>The deck is face down and waiting for the cutter to choose.</summary>
    Cutting,

    Dealing,

    Playing
}

/// <summary>Where a player took their card from.</summary>
public enum DrawnFrom
{
    Stock,

    /// <summary>
    /// The card the player before them discarded. Only the top of the pile is
    /// ever on offer; what lies under it has been played past.
    /// </summary>
    Discards
}

/// <summary>
/// One hand, from the shuffle to its end. It knows nothing of the table's
/// mode: a single hand, one of a survival run and the third of a series are
/// the same hand played the same way, and what follows it is somebody else's
/// question.
/// </summary>
public sealed class Hand
{

    private readonly List<List<HeldCard>> hands;

    /// <summary>What this turn has done and can still take back.</summary>
    private readonly TurnSoFar turn = new();

    /// <summary>
    /// Who has shown a hand. Until a player has, everything they hold costs
    /// them a flat hundred however cheap it is.
    /// </summary>
    private readonly bool[] wentDown;
    private IReadOnlyList<Card> deck;
    private Cut? cut;
    private int given;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
        hands = [.. Enumerable.Range(0, seats).Select(_ => new List<HeldCard>())];
        wentDown = new bool[seats];
        DealerSeat = dealerSeat;
        CutterSeat = (dealerSeat + seats - 1) % seats;
        Seed = seed;

        var fresh = Deck.New(jokers);
        var backs = Deck.Backs(jokers);

        // One shuffle, applied to both, so every card keeps the back of the
        // pack it came from. Shuffling the backs separately would be a lie
        // about what two packs are, and would not come out even.
        var order = Deck.Shuffle<int>([.. Enumerable.Range(0, fresh.Count)], seed);

        deck = [.. order.Select(index => fresh[index])];
        Backs = [.. order.Select(index => backs[index])];
        DeckSize = deck.Count;

        // Taken now, because the cut rearranges the deck and would move them.
        JokerPositions =
            [.. deck.Select((card, at) => (card, at)).Where(e => e.card is Card.Joker).Select(e => e.at)];
    }

    public int Seats { get; }
    public int DealerSeat { get; }
    public int CutterSeat { get; }

    /// <summary>Kept so a hand that behaved oddly can be dealt again exactly.</summary>
    public int Seed { get; }

    /// <summary>How many cards are face down for the cutter to choose from.</summary>
    public int DeckSize { get; }

    /// <summary>One or two for each card in the deck, by position.</summary>
    public IReadOnlyList<int> Backs { get; }

    /// <summary>
    /// Where the jokers are lying in the deck. It is the one thing here that
    /// nobody at the table may know, and it reaches a client only where a
    /// development setting has asked for it so the cut can be tested.
    /// </summary>
    public IReadOnlyList<int> JokerPositions { get; }

    public HandPhase Phase { get; private set; } = HandPhase.Cutting;

    /// <summary>Whose turn it is, or -1 before play has begun.</summary>
    public int TurnSeat { get; private set; } = -1;

    /// <summary>
    /// Whether whoever is sitting there has taken their card yet. A turn is a
    /// card in and a card out, and this is which half of it is left.
    /// </summary>
    public bool HasDrawn { get; private set; }

    /// <summary>
    /// Whose move the hand is waiting for, or -1 when it is waiting for
    /// nobody - which includes the deal, where the cards are going out and
    /// there is nobody to hurry. How long it waits is the table's business
    /// and not the hand's; who it waits for is a rule of the game and is
    /// here.
    /// </summary>
    public int WaitingSeat => Phase switch
    {
        HandPhase.Cutting => CutterSeat,
        HandPhase.Playing => TurnSeat,
        _ => -1,
    };

    public Card? FinishingCard => cut?.FinishingCard;

    /// <summary>
    /// Where in the deck the cutter chose, counted from the top as the deck
    /// lay before them. Null until they have. Everyone at the table needs it:
    /// without it only the one who clicked knows which card turned over.
    /// </summary>
    public int? CutPosition { get; private set; }

    public IReadOnlyList<Card> RevealedAtCut => cut?.Revealed ?? [];

    /// <summary>
    /// What each seat is holding, in the order it arrived. Every list is
    /// empty until the deal. This is the one thing a hand knows that belongs
    /// to one player rather than to the table.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<HeldCard>> Hands => hands;

    /// <summary>
    /// Everything lying on the felt. A hand says who may reach for it and
    /// when; what is there is the board's own.
    /// </summary>
    public Board Board { get; } = new();

    /// <summary>Who has gone down, which the whole table may see.</summary>
    public IReadOnlyList<bool> WentDown => wentDown;

    /// <summary>
    /// What that hand would cost its owner if the hand ended now: a flat
    /// hundred until they have gone down, and after that what they hold.
    /// </summary>
    public int WorthOf(int seat) =>
        wentDown[seat] ? Worth.Held(hands[seat].Select(card => card.Card)) : Worth.NeverWentDown;

    /// <summary>
    /// Whether this seat could end its turn on what it has laid. A player
    /// who has laid nothing may always end it - nobody has to go down - and
    /// one who is down already may lay whatever is a meld. It is the first
    /// showing that has to stand up, and what makes one is a rule and lives
    /// with the rules.
    /// </summary>
    public bool CanEndTheTurn(int seat) =>
        wentDown[seat]
        || turn.Laying.Count == 0
        || Melding.IsAShowing([.. turn.Laying.Select(Lying)]);

    /// <summary>The deal as the eye should see it, pass by pass.</summary>
    public IReadOnlyList<DealPass> Passes { get; private set; } = [];

    /// <summary>
    /// The cutter chose a card. False when the hand is past this point, which
    /// is what a second click on a deck already cut looks like from here.
    /// </summary>
    public bool CutAt(int position)
    {
        if (Phase != HandPhase.Cutting)
        {
            return false;
        }

        cut = Cutting.At(deck, position);
        CutPosition = ((position % DeckSize) + DeckSize) % DeckSize;
        deck = cut.Remaining;
        Phase = HandPhase.Dealing;

        return true;
    }

    /// <summary>
    /// Play begins. The turn goes to the player after the dealer, who is the
    /// one holding fifteen cards - and that fifteenth card is their draw,
    /// taken during the deal. So they open their turn already halfway through
    /// it, with nothing left to do but discard.
    /// False when the cards have not been dealt, or play has begun already.
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

    /// <summary>
    /// Deals what the cut left. A joker the cut gave the cutter is already
    /// theirs and opens the hand they are dealt the rest of, which is also
    /// why they are dealt one card fewer for each of them. False when the
    /// hand has not been cut, or has been dealt already.
    /// </summary>
    public bool Deal()
    {
        if (Phase != HandPhase.Dealing || Passes.Count > 0)
        {
            return false;
        }

        var dealt = Dealing.Deal(deck, Seats, DealerSeat, RevealedAtCut.Count);

        for (var seat = 0; seat < Seats; seat++)
        {
            if (seat == CutterSeat)
            {
                Give(seat, RevealedAtCut);
            }

            Give(seat, dealt.Hands[seat]);
        }

        Board.StockWith(dealt.Stock);
        Passes = dealt.Passes;

        return true;
    }

    /// <summary>
    /// Takes a card, which is the first half of a turn. False when it is not
    /// that seat's turn, when they have taken one already, or when there is
    /// nothing where they reached.
    /// </summary>
    public bool Draw(int seat, DrawnFrom from)
    {
        if (!IsTheirTurn(seat) || HasDrawn)
        {
            return false;
        }

        var taken = from == DrawnFrom.Stock ? Board.TakeFromStock() : Board.TakeFromDiscards();

        if (taken is null)
        {
            return false;
        }

        // Read before the card is given one, which is the number it will
        // be given, and the only way to find it again afterwards.
        var number = given;

        Give(seat, [taken]);
        HasDrawn = true;
        turn.Took(number, from);

        return true;
    }

    /// <summary>
    /// Throws a card away, which ends the turn. The card is named by the
    /// number it was given rather than by what it is: two sevens of hearts
    /// are the same card, and a player throwing one of them means that one.
    /// False when it is not that seat's turn, when they have not taken a card
    /// yet, or when they are not holding the card they named.
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
        HandItOn();

        return true;
    }

    /// <summary>
    /// A turn nobody took. Everything it did is taken back first - the runs
    /// and sets it laid, and the card it took off the pile, which goes back
    /// where it came from - and then a card off the stock goes straight onto
    /// the pile. Whoever walked away comes back to the hand they left, one
    /// card of the stock the poorer, and the table as they found it.
    ///
    /// How long a table waits before deciding a turn is nobody's is the
    /// table's own business. What happens then is a rule of the game, so it
    /// is here.
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
    /// The card this turn drew, which is the one that leaves the hand as its
    /// owner left it. Whoever opens the hand drew theirs during the deal and
    /// has no such card, so for them it is the last one they were dealt.
    /// </summary>
    private int WhatCameOffTheStock(int seat) => turn.OffTheStock ?? hands[seat][^1].Id;

    /// <summary>
    /// The turn passed on, which is the only way one ever ends. Nothing of
    /// it is left to take back in the next.
    /// </summary>
    private void HandItOn()
    {
        TurnSeat = (TurnSeat + 1) % Seats;
        HasDrawn = false;
        Forget();
    }

    /// <summary>
    /// Lays cards on the table, which is the move between taking one and
    /// throwing one. It does not ask whether they are a run or a set, or
    /// whether they are worth fifty-one between them: those are the rules of
    /// going down and they are not written yet. What it does ask is that the
    /// cards are in that player's hand, and that it is their turn to be
    /// laying anything at all.
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

        var meld = new Meld(seat, [.. places.Select(at => held[at])]);

        // What goes down has to be a run or a set. Nothing else is a meld,
        // and the table is not somewhere to put cards that are not one.
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
    /// A card out of a hand and onto a meld already lying on the table. It
    /// asks that the card fits where it is being put, which is a rule of
    /// the game and is answered in one place; it does not yet ask whether
    /// this player has gone down and is entitled to be adding to anything
    /// at all, which is the other half of going down and is not written.
    /// </summary>
    public bool AddTo(int seat, int meldAt, int cardId, MeldEnd end)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || meldAt < 0 || meldAt >= Board.Melds.Count)
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
    /// A joker bought out of a meld with the card it was standing in for.
    /// The card goes where the joker was lying and the joker comes into the
    /// hand that paid for it, keeping the number it has had since it was
    /// dealt. A card is given its number once and carries it wherever it
    /// goes, which is what lets the screen follow one across the table.
    ///
    /// It does not yet ask whether this player has gone down and is entitled
    /// to be buying anything, nor hold them to giving the joker back before
    /// the turn ends. Both are the other half of going down, and neither is
    /// written.
    /// </summary>
    public bool SwapForJoker(int seat, int meldAt, int at, int cardId)
    {
        if (!IsTheirTurn(seat) || !HasDrawn || meldAt < 0 || meldAt >= Board.Melds.Count)
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
    /// Whether the player whose turn it is has anything left to take back.
    /// Everybody is told: a meld being laid and then taken up again is
    /// something the whole table watched happen.
    /// </summary>
    public bool CanUndo => turn.Anything;

    /// <summary>
    /// Takes the turn back: every meld it laid, last first, and after them
    /// the card it took off the discard pile. All of it and not the last
    /// thing alone, because what a player wants back is the move, and
    /// taking it apart a piece at a time is the same decision undone four
    /// times.
    ///
    /// The melds come first because they came last, and because one of them
    /// may hold the very card that was taken off the pile.
    ///
    /// A card off the stock is never taken back. Putting it where it came
    /// from would let a player look at the top of the stock and then decide
    /// not to have looked, and there is no undoing knowing something.
    /// </summary>
    public bool Undo(int seat)
    {
        if (!IsTheirTurn(seat))
        {
            return false;
        }

        var anything = false;

        while (TakeBackTheLastThing(seat) || PutTheDiscardBack(seat))
        {
            anything = true;
        }

        return anything;
    }

    /// <summary>
    /// The last thing put on the table, picked up again. A meld comes back
    /// whole to the hand that laid it; a card added to somebody else's meld
    /// comes back on its own.
    /// </summary>
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
    /// The card is found by the number it was given when it was taken, so
    /// the one that goes back is the one that came - and not its twin out
    /// of the other pack, which the player may have been holding all along.
    /// </summary>
    private bool PutTheDiscardBack(int seat)
    {
        if (turn.TheDiscard is not { } number)
        {
            return false;
        }

        var at = hands[seat].FindIndex(card => card.Id == number);

        if (at == -1)
        {
            return false;
        }

        Board.Throw(hands[seat][at].Card);
        hands[seat].RemoveAt(at);
        HasDrawn = false;
        turn.GaveItBack();

        return true;
    }

    /// <summary>Nothing of one turn is left to take back in the next.</summary>
    private void Forget() => turn.Forget();

    private bool IsTheirTurn(int seat) => Phase == HandPhase.Playing && seat == TurnSeat;

    /// <summary>The cards of one meld as the rules read them.</summary>
    private IReadOnlyList<Card> Lying(int at) =>
        [.. Board.Melds[at].Cards.Select(card => card.Card)];

    /// <summary>
    /// Cards reaching a hand, each numbered as it arrives. That number is
    /// what the player's own arrangement is held by, so it has to outlive
    /// every sort and every move and is never given out twice.
    /// </summary>
    private void Give(int seat, IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            hands[seat].Add(new HeldCard(given++, card));
        }
    }

}
