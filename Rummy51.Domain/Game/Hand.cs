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
    private readonly List<Meld> melds = [];

    /// <summary>
    /// What this turn has done that can still be taken back, newest last.
    /// Only melds go in it: a card off the stock is not in here because it
    /// cannot come back out, and a card off the discard pile is the single
    /// flag below rather than an entry, since there is at most one draw in
    /// a turn and it is always the first thing in it.
    /// </summary>
    private readonly List<Meld> laidThisTurn = [];

    /// <summary>
    /// The number given to the card taken off the discard pile this turn,
    /// if that is where the draw came from. By number and not by position:
    /// a meld taken back puts its cards at the end of the hand, so the card
    /// that was drawn is not the last one for long.
    /// </summary>
    private int? theDiscardTaken;
    private readonly List<Card> stock = [];
    private readonly List<Card> discards = [];
    private IReadOnlyList<Card> deck;
    private Cut? cut;
    private int given;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
        hands = [.. Enumerable.Range(0, seats).Select(_ => new List<HeldCard>())];
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

    /// <summary>What is left to draw from once the deal is done.</summary>
    public IReadOnlyList<Card> Stock => stock;

    /// <summary>
    /// What has been thrown away, oldest first. Only the last of them is any
    /// player's business, and that is all the table is ever told.
    /// </summary>
    public IReadOnlyList<Card> Discards => discards;

    /// <summary>The one card of the pile lying face up, or null before any.</summary>
    public Card? TopOfDiscards => discards.Count == 0 ? null : discards[^1];

    /// <summary>
    /// What is lying on the table, in the order it was laid there. Nobody
    /// owns it any more in the sense of holding it, but whose it was still
    /// matters, so each one says.
    /// </summary>
    public IReadOnlyList<Meld> Melds => melds;

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

        stock.AddRange(dealt.Stock);
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

        var taken = from == DrawnFrom.Stock ? TakeFromStock() : TakeFromDiscards();

        if (taken is null)
        {
            return false;
        }

        // Read before the card is given one, which is the number it will
        // be given, and the only way to find it again afterwards.
        var number = given;

        Give(seat, [taken]);
        HasDrawn = true;
        theDiscardTaken = from == DrawnFrom.Discards ? number : null;

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
        if (!IsTheirTurn(seat) || !HasDrawn)
        {
            return false;
        }

        var held = hands[seat].FindIndex(card => card.Id == cardId);

        if (held == -1)
        {
            return false;
        }

        discards.Add(hands[seat][held].Card);
        hands[seat].RemoveAt(held);
        TurnSeat = (TurnSeat + 1) % Seats;
        HasDrawn = false;
        Forget();

        return true;
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

        melds.Add(meld);
        laidThisTurn.Add(meld);

        // From the back, so that taking one out does not move the places of
        // those still to be taken.
        foreach (var at in places.OrderDescending())
        {
            held.RemoveAt(at);
        }

        return true;
    }

    /// <summary>
    /// Whether the player whose turn it is has anything left to take back.
    /// Everybody is told: a meld being laid and then taken up again is
    /// something the whole table watched happen.
    /// </summary>
    public bool CanUndo => laidThisTurn.Count > 0 || theDiscardTaken is not null;

    /// <summary>
    /// Takes back the last thing this turn did, which is the last meld laid
    /// or, once they are all back, the card taken off the discard pile.
    ///
    /// A card off the stock is never taken back. Putting it where it came
    /// from would let a player look at the top of the stock and then decide
    /// not to have looked, and there is no undoing knowing something.
    ///
    /// The melds come back first because they came last, and because one of
    /// them may hold the very card that was taken off the pile.
    /// </summary>
    public bool Undo(int seat)
    {
        if (!IsTheirTurn(seat))
        {
            return false;
        }

        return TakeBackTheLastMeld() || PutTheDiscardBack(seat);
    }

    private bool TakeBackTheLastMeld()
    {
        if (laidThisTurn.Count == 0)
        {
            return false;
        }

        var meld = laidThisTurn[^1];

        laidThisTurn.RemoveAt(laidThisTurn.Count - 1);
        melds.Remove(meld);
        hands[meld.Seat].AddRange(meld.Cards);

        return true;
    }

    /// <summary>
    /// The card is found by the number it was given when it was taken, so
    /// the one that goes back is the one that came - and not its twin out
    /// of the other pack, which the player may have been holding all along.
    /// </summary>
    private bool PutTheDiscardBack(int seat)
    {
        if (theDiscardTaken is not { } number)
        {
            return false;
        }

        var at = hands[seat].FindIndex(card => card.Id == number);

        if (at == -1)
        {
            return false;
        }

        discards.Add(hands[seat][at].Card);
        hands[seat].RemoveAt(at);
        HasDrawn = false;
        theDiscardTaken = null;

        return true;
    }

    /// <summary>Nothing of one turn is left to take back in the next.</summary>
    private void Forget()
    {
        laidThisTurn.Clear();
        theDiscardTaken = null;
    }

    private bool IsTheirTurn(int seat) => Phase == HandPhase.Playing && seat == TurnSeat;

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

    /// <summary>The front, which is where the deal left off.</summary>
    private Card? TakeFromStock()
    {
        if (stock.Count == 0)
        {
            return null;
        }

        var card = stock[0];

        stock.RemoveAt(0);

        return card;
    }

    /// <summary>The back, which is where the player before threw.</summary>
    private Card? TakeFromDiscards()
    {
        if (discards.Count == 0)
        {
            return null;
        }

        var card = discards[^1];

        discards.RemoveAt(discards.Count - 1);

        return card;
    }
}
