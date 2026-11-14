namespace Rummy51.Domain.Game;

/// <summary>
/// Chapter 12: a redeal offered, answered, read, and then dealt. It is a
/// stage of a hand rather than a thing beside one - it hands the same cards
/// round the same table and leaves the same hand playing - so it is the
/// same class, in a file of its own because it is a long stage.
/// </summary>
public sealed partial class Hand
{
    private readonly bool[] hasOffered;
    private readonly int[]?[] keeping;
    private Offer? offer;
    private bool dealingAgain;

    /// <summary>How many of their own a player keeps through a redeal. Rule 12.6.</summary>
    public const int Kept = 3;

    /// <summary>
    /// Rules 12.1, 12.2. The finishing card is not in flight either: taking
    /// it promised the table a win, and a promise is not unmade by a vote.
    /// </summary>
    public bool CanOfferARedeal(int seat) =>
        IsTheirTurn(seat)
        && HasDrawn
        && !hasOffered[seat]
        && !PromisedToGoOut
        && !turn.PutAnythingDown;

    public bool OfferARedeal(int seat)
    {
        if (!CanOfferARedeal(seat))
        {
            return false;
        }

        hasOffered[seat] = true;
        offer = new Offer(seat, Seats);
        Phase = HandPhase.Offering;

        return true;
    }

    /// <summary>Who offered, or -1 when nothing is on the table to answer.</summary>
    public int OfferedBy => offer?.By ?? -1;

    /// <summary>Rule 12.4: everything but the offerer's is kept from whoever has not answered.</summary>
    public IReadOnlyList<bool?> OfferAsSeenBy(int seat) => offer?.AsSeenBy(seat) ?? [];

    /// <summary>Rule 12.3.</summary>
    public bool Answer(int seat, bool agreed)
    {
        if (Phase != HandPhase.Offering || offer?.Answer(seat, agreed) != true)
        {
            return false;
        }

        if (!offer.Open)
        {
            Phase = HandPhase.Answered;
        }

        return true;
    }

    /// <summary>Rule 12.4.</summary>
    public bool TakeSilenceAsAgreement()
    {
        if (Phase != HandPhase.Offering || offer is null)
        {
            return false;
        }

        offer.TakeSilenceAsAgreement();
        Phase = HandPhase.Answered;

        return true;
    }

    /// <summary>
    /// The result read. Rule 12.4 puts it in front of everybody before
    /// anything moves, so that a refusal is never anonymous.
    /// </summary>
    public bool Read(int seat)
    {
        if (Phase != HandPhase.Answered || offer?.Read(seat) != true)
        {
            return false;
        }

        if (offer.EverybodyHasRead)
        {
            Settle();
        }

        return true;
    }

    /// <summary>Long enough to have read it, whether anybody did or not.</summary>
    public bool StopShowingTheResult()
    {
        if (Phase != HandPhase.Answered)
        {
            return false;
        }

        Settle();

        return true;
    }

    /// <summary>
    /// Rule 12.3 sends the turn back where it was, and rule 12.5 hands every
    /// card on the table to whoever put it there.
    /// </summary>
    private void Settle()
    {
        var agreed = offer?.Agreed == true;

        offer = null;

        if (!agreed)
        {
            Phase = HandPhase.Playing;

            return;
        }

        foreach (var card in Board.ClearTheTable().SelectMany(meld => meld.Cards))
        {
            hands[card.Owner].Add(card);
        }

        Phase = HandPhase.Keeping;
    }

    /// <summary>Rule 12.6: exactly three, and their own.</summary>
    public bool Keep(int seat, IReadOnlyList<int> cardIds)
    {
        if (Phase != HandPhase.Keeping || seat < 0 || seat >= Seats || keeping[seat] is not null)
        {
            return false;
        }

        if (cardIds.Count != Kept || cardIds.Distinct().Count() != Kept)
        {
            return false;
        }

        if (cardIds.Any(id => hands[seat].All(card => card.Id != id)))
        {
            return false;
        }

        keeping[seat] = [.. cardIds];

        if (keeping.All(three => three is not null))
        {
            DealAgain();
        }

        return true;
    }

    /// <summary>
    /// Rule 12.6 for whoever has not chosen. The dearest three, which puts
    /// the jokers first: twenty in the hand is the most any card is worth,
    /// so the one sort answers both.
    /// </summary>
    public bool KeepForWhoeverHasNot()
    {
        if (Phase != HandPhase.Keeping)
        {
            return false;
        }

        for (var seat = 0; seat < Seats; seat++)
        {
            keeping[seat] ??=
            [
                .. hands[seat]
                    .OrderByDescending(card => Worth.Held(card.Card))
                    .Take(Kept)
                    .Select(card => card.Id)
            ];
        }

        DealAgain();

        return true;
    }

    /// <summary>
    /// Rules 12.6, 12.7 and 12.8. Everything but the kept cards goes into a
    /// deck again and the hand starts at the cut, so the cutter cuts and the
    /// deal runs exactly as they did the first time - one round shorter.
    /// </summary>
    private void DealAgain()
    {
        var loose = new List<Card>(Board.Stock);

        loose.AddRange(Board.Discards);

        if (Deck.FinishingCard is { } finishing)
        {
            loose.Add(finishing);
        }

        for (var seat = 0; seat < Seats; seat++)
        {
            var keep = keeping[seat]!.ToHashSet();

            loose.AddRange(hands[seat].Where(card => !keep.Contains(card.Id)).Select(card => card.Card));
            hands[seat].RemoveAll(card => !keep.Contains(card.Id));
            keeping[seat] = null;
        }

        Deck = new Deck(loose, Deck.Seed);
        Board = new Board();
        Passes = [];

        Array.Fill(wentDown, false);
        Array.Fill(hasOffered, false);

        turnsTaken = 0;
        TurnSeat = -1;
        HasDrawn = false;
        dealingAgain = true;
        Phase = HandPhase.Cutting;

        Forget();
    }
}
