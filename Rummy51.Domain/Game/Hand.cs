namespace Rummy51.Domain.Game;

public enum HandPhase
{
    /// <summary>The deck is face down and waiting for the cutter to choose.</summary>
    Cutting,

    Dealing,

    Playing
}

/// <summary>
/// One hand, from the shuffle to its end. It knows nothing of the table's
/// mode: a single hand, one of a survival run and the third of a series are
/// the same hand played the same way, and what follows it is somebody else's
/// question.
/// </summary>
public sealed class Hand
{
    private IReadOnlyList<Card> deck;
    private IReadOnlyList<IReadOnlyList<Card>> hands;
    private Cut? cut;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
        hands = [.. Enumerable.Range(0, seats).Select(IReadOnlyList<Card> (_) => [])];
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
    public IReadOnlyList<IReadOnlyList<Card>> Hands => hands;

    /// <summary>What is left to draw from once the deal is done.</summary>
    public IReadOnlyList<Card> Stock { get; private set; } = [];

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
    /// Play begins. The turn goes to the player after the dealer, who is
    /// the one holding fifteen cards and the one who opens by discarding.
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

        hands = [.. dealt.Hands.Select(IReadOnlyList<Card> (cards, seat) =>
            seat == CutterSeat ? [.. RevealedAtCut, .. cards] : cards)];
        Stock = dealt.Stock;
        Passes = dealt.Passes;

        return true;
    }
}
