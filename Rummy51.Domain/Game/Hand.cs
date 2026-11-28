namespace Rummy51.Domain.Game;

public enum HandPhase
{
    Cutting,

    Dealing,

    Playing,

    Offering,

    Answered,

    Keeping,

    /// <summary>Rule 3.5.</summary>
    Restocking,

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
/// What is here is what a hand is: the cards it holds, the seat whose turn
/// it is, the phase it is in, and the few helpers everything else reaches
/// for. Each chapter it enforces sits beside this in a file of its own,
/// each declaring the fields it alone uses - Dealing, Turn, Laying, Undo,
/// Allowed, Scoring, and chapter 12 across Redeal, DealAgain and Force.
///
/// They are one class and not several because a redeal, a turn and a score
/// are stages of the same hand rather than things of their own, and a hand
/// split into objects would have to hand its cards between them.
/// </summary>
public sealed partial class Hand
{
    private readonly List<List<HeldCard>> hands;
    private readonly TurnSoFar turn = new();
    private readonly bool[] playing;
    private readonly bool[] wasDown;
    private readonly bool[] wentRummy;
    private readonly bool[] tookTheFinishingCard;

    /// <summary>Rule 3.5, by seat, and put back once the stock has been filled.</summary>
    private readonly bool[] seenTheRestock;

    /// <summary>
    /// Rule 3.5's shuffle, running on from the deal's own seed. A seed does
    /// not reproduce a restock whatever it is - what goes into one is
    /// whatever the table happened to throw - so the order each one came out
    /// in is kept instead, and this only has to be a shuffle nobody chose.
    /// </summary>
    private readonly Random shuffling;

    /// <summary>Rule 3.5: what each restock came out as, in the order they happened.</summary>
    private readonly List<string> restocks = [];
    private bool downBeforeThisTurn;

    /// <summary>
    /// How the first round is counted. A turn taken back and taken again is
    /// still one turn: seats that have had their go, not moves made.
    /// </summary>
    private int turnsTaken;
    private int given;

    public Hand(int seats, int jokers, int dealerSeat, int seed, IReadOnlyList<bool>? playing = null)
        : this(seats, dealerSeat, new Deck(jokers, seed), playing)
    {
    }

    /// <summary>
    /// A hand dealt from a deck somebody else laid out, which only a
    /// development aid asks for: every other hand makes its own.
    ///
    /// Whatever the hand it replaces has already been through comes with
    /// it, because rule 12.8 leaves a hand that was redealt the same hand
    /// and chapters 8 and 12 both read that at the end. It is taken whole
    /// rather than field by field on purpose: a carry that lists what to
    /// bring is a carry that forgets the next thing added beside it, which
    /// is how the redeals came to be written down and then thrown away.
    /// </summary>
    public Hand(
        int seats,
        int dealerSeat,
        Deck deck,
        IReadOnlyList<bool>? playing = null,
        Hand? was = null)
    {
        Seats = seats;
        this.playing = [.. playing ?? was?.Playing ?? Enumerable.Repeat(true, seats)];
        hands = [.. Enumerable.Range(0, seats).Select(_ => new List<HeldCard>())];
        wasDown = new bool[seats];
        wentRummy = new bool[seats];
        tookTheFinishingCard = new bool[seats];
        seenTheRestock = new bool[seats];
        hasOffered = new bool[seats];
        hasPlayed = new bool[seats];
        keeping = new int[seats][];
        DealerSeat = dealerSeat;
        CutterSeat = Dealing.Before(dealerSeat, this.playing);
        Deck = deck;
        shuffling = new Random(deck.Seed);

        if (was is null)
        {
            return;
        }

        Weight = was.Weight;
        AgreedRedeals = was.AgreedRedeals;
        ForcedRedeals = was.ForcedRedeals;
        redeals.AddRange(was.Redeals);
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

    /// <summary>
    /// Which seats are in this hand. A seat that is out keeps its place -
    /// everything here counts seats by where they sit - and is dealt
    /// nothing, given no turn, and stepped over by rules 2.1 and 3.2.
    /// </summary>
    public IReadOnlyList<bool> Playing => playing;

    /// <summary>How many are in it, which is what a round is counted in.</summary>
    public int Players => playing.Count(one => one);

    /// <summary>Rule 11.3: their runs and sets are lying there to be read.</summary>
    public IReadOnlyList<bool> WentDown => [.. Enumerable.Range(0, Seats).Select(IsDown)];

    /// <summary>Rule 6.6.</summary>
    public IReadOnlyList<bool> WentRummy => wentRummy;

    /// <summary>Rule 3.5: who has said they have seen the stock filled.</summary>
    public IReadOnlyList<bool> SeenTheRestock => seenTheRestock;

    /// <summary>Rule 3.5, which rule 11.5 needs to deal this hand again.</summary>
    public IReadOnlyList<string> Restocks => restocks;

    /// <summary>Rule 7.1, which only one player a hand can ever have done.</summary>
    public IReadOnlyList<bool> TookTheFinishingCard => tookTheFinishingCard;

    /// <summary>Rule 8.2 doubles on the first of these and not the second.</summary>
    public int AgreedRedeals { get; private set; }

    public int ForcedRedeals { get; private set; }

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
