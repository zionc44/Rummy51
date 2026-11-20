namespace Rummy51.Domain.Game;

/// <summary>
/// Rules 12.1 to 12.4: a redeal offered, answered and read. What one that
/// was taken then costs is the deal's and sits beside this. It is a stage
/// of a hand rather than a thing beside one - it hands the same cards round
/// the same table and leaves the same hand playing - so it is the same
/// class, in a file of its own because it is a long stage.
/// </summary>
public sealed partial class Hand
{
    private readonly bool[] hasOffered;
    private readonly List<Redeal> redeals = [];
    private Offer? offer;

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

    public bool OfferARedeal(int seat, DateTimeOffset at)
    {
        if (!CanOfferARedeal(seat))
        {
            return false;
        }

        hasOffered[seat] = true;
        offer = new Offer(seat, playing, at);
        Phase = HandPhase.Offering;

        return true;
    }

    /// <summary>Who offered, or -1 when nothing is on the table to answer.</summary>
    public int OfferedBy => offer?.By ?? -1;

    /// <summary>
    /// Every redeal this hand has finished with, in the order they were put
    /// to the table. It survives a deal again on purpose: rule 12.8 leaves
    /// the hand the same one, so what was asked of it earlier is still its.
    /// </summary>
    public IReadOnlyList<Redeal> Redeals => redeals;

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
    public bool Read(int seat, DateTimeOffset at)
    {
        if (Phase != HandPhase.Answered || offer?.Read(seat) != true)
        {
            return false;
        }

        if (offer.EverybodyHasRead)
        {
            Settle(at);
        }

        return true;
    }

    /// <summary>Long enough to have read it, whether anybody did or not.</summary>
    public bool StopShowingTheResult(DateTimeOffset at)
    {
        if (Phase != HandPhase.Answered)
        {
            return false;
        }

        Settle(at);

        return true;
    }

    /// <summary>
    /// Rule 12.3 sends the turn back where it was, and rule 12.5 hands every
    /// card on the table to whoever put it there.
    /// </summary>
    private void Settle(DateTimeOffset at)
    {
        var agreed = offer?.Agreed == true;
        var forced = offer?.Forced == true;

        if (offer is not null)
        {
            // Before rule 12.5 moves anything: the pairs are read out of the
            // forcer's hand, and in a moment the table goes back into it.
            redeals.Add(new Redeal(
                offer.By,
                forced,
                offer.AtOnce,
                forced ? FourPairsOf(offer.By) : [],
                agreed,
                offer.Replies,
                offer.AskedAt,
                at));
        }

        offer = null;

        // Rule 12.3: an offer nobody was made to take leaves the table as it
        // found it. Rule 12.11: a forced one deals again either way.
        if (!agreed && !forced)
        {
            Phase = HandPhase.Playing;

            return;
        }

        foreach (var card in Board.ClearTheTable().SelectMany(meld => meld.Cards))
        {
            hands[card.Owner].Add(card);
        }

        if (agreed)
        {
            AgreedRedeals += 1;
            Phase = HandPhase.Keeping;

            return;
        }

        ForcedRedeals += 1;
        DealAgainKeepingNothing();
    }
}
