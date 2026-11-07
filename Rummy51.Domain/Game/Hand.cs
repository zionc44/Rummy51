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
    private Cut? cut;

    public Hand(int seats, int jokers, int dealerSeat, int seed)
    {
        Seats = seats;
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

    public HandPhase Phase { get; private set; } = HandPhase.Cutting;

    public Card? FinishingCard => cut?.FinishingCard;

    public IReadOnlyList<Card> RevealedAtCut => cut?.Revealed ?? [];

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
        deck = cut.Remaining;
        Phase = HandPhase.Dealing;

        return true;
    }
}
