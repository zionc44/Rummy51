using Rummy51.Domain.Entities;
using static Rummy51.Domain.Tests.Cards;

namespace Rummy51.Domain.Tests;

/// <summary>
/// Rules 12.9 to 12.13: a redeal nobody gets to refuse outright. What is
/// its own is who may force one and what they are holding; everything after
/// that is the offer of 12.1 settling in the same place.
/// </summary>
public class TheForcedRedeal
{
    private static readonly DateTimeOffset Asked = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Settled = Asked.AddSeconds(9);

    /// <summary>
    /// Rule 12.9: the same card twice, four times over. The seven after it
    /// pair with nothing, so the hand holds four pairs and not five.
    /// </summary>
    private static IReadOnlyList<Card> FourPairs =>
    [
        S(4), S(4), H(9), H(9), C(King), C(King), D(8), D(8),
        S(2), H(3), C(5), D(6), S(10), H(Jack), C(Queen),
    ];

    [Fact]
    public void Rule_12_9_four_pairs_may_force_a_redeal_on_the_table()
    {
        var hand = Dealt.Holding(FourPairs);

        Assert.Equal(8, hand.FourPairsOf(hand.TurnSeat).Count);
        Assert.True(hand.CanForceARedeal(hand.TurnSeat));
    }

    [Fact]
    public void Rule_12_9_and_an_ordinary_hand_may_not()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);

        Assert.False(hand.CanForceARedeal(hand.TurnSeat));
    }

    [Fact]
    public void Rule_12_9_two_jokers_are_not_a_pair()
    {
        // A joker is neither a rank nor a suit, however alike two of them
        // look, so this hand holds three pairs and a pair of nothing.
        var hand = Dealt.Holding(
            [S(4), S(4), H(9), H(9), C(King), C(King), Joker(1), Joker(2),
             S(2), H(3), C(5), D(6), S(10), H(Jack), C(Queen)]);

        Assert.False(hand.CanForceARedeal(hand.TurnSeat));
    }

    [Fact]
    public void Rule_12_9_it_is_theirs_whether_or_not_they_have_offered_one()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var holder = hand.TurnSeat;

        hand.OfferARedeal(holder, Asked);
        hand.Answer((holder + 1) % 3, agreed: false);
        hand.Answer((holder + 2) % 3, agreed: true);
        hand.StopShowingTheResult(Settled);

        // Rule 12.2 has closed the offer to them for this hand. Rule 12.9
        // is a different right and is still theirs.
        Assert.False(hand.CanOfferARedeal(holder));
        Assert.True(hand.CanForceARedeal(holder));
    }

    [Fact]
    public void Rule_12_9_and_does_not_last_past_their_own_first_turn()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var holder = hand.TurnSeat;

        hand.Discard(holder, hand.Hands[holder][^1].Id);

        Assert.False(hand.CanForceARedeal(holder));
    }

    [Fact]
    public void Rule_12_13_put_to_the_table_it_is_answered_like_any_other()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        Assert.True(hand.ForceARedeal(by, atOnce: false, Asked));
        Assert.Equal(HandPhase.Offering, hand.Phase);
        Assert.True(hand.Forced);
    }

    [Fact]
    public void Rule_12_10_or_ended_there_and_then_with_nothing_to_ask()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        hand.ForceARedeal(by, atOnce: true, Asked);

        Assert.Equal(HandPhase.Answered, hand.Phase);

        hand.StopShowingTheResult(Settled);

        var forced = hand.Redeals.Single();

        Assert.True(forced.Forced);
        Assert.True(forced.AtOnce);
        Assert.Equal(RedealReply.Offered, forced.Replies[by]);
        Assert.All(
            Enumerable.Range(0, 3).Where(seat => seat != by),
            seat => Assert.Equal(RedealReply.NotAsked, forced.Replies[seat]));
    }

    [Fact]
    public void Rule_12_11_it_is_dealt_again_however_it_was_answered()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        hand.ForceARedeal(by, atOnce: false, Asked);

        foreach (var seat in Enumerable.Range(0, 3).Where(one => one != by))
        {
            hand.Answer(seat, agreed: false);
        }

        hand.StopShowingTheResult(Settled);

        // Refused by everybody who was asked, and dealt again anyway.
        Assert.Equal(HandPhase.Cutting, hand.Phase);
        Assert.Equal(1, hand.ForcedRedeals);
    }

    [Fact]
    public void Rule_8_2_and_a_forced_redeal_does_not_double()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        hand.ForceARedeal(by, atOnce: true, Asked);
        hand.StopShowingTheResult(Settled);

        Assert.Equal(1, hand.ForcedRedeals);
        Assert.Equal(0, hand.AgreedRedeals);
        Assert.Equal(1, hand.RedealMultiplier);
    }

    [Fact]
    public void Rule_12_12_nobody_keeps_anything_and_nobody_is_asked()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        hand.ForceARedeal(by, atOnce: true, Asked);
        hand.StopShowingTheResult(Settled);

        // Straight to the cut: there is no keeping to be done.
        Assert.Equal(HandPhase.Cutting, hand.Phase);

        hand.CutAt(7);
        hand.Deal();

        // And the hand that comes out is a whole one, not twelve and eleven
        // on top of three kept.
        Assert.Equal(15, hand.Hands[Dealing.After(hand.DealerSeat, hand.Playing)].Count);
    }

    [Fact]
    public void Rule_12_11_the_pairs_are_shown_once_the_table_may_see_them()
    {
        var hand = Dealt.Holding(FourPairs, seats: 3);
        var by = hand.TurnSeat;

        hand.ForceARedeal(by, atOnce: false, Asked);

        // Rule 11.1 covers them while the question is open: a threat is
        // worth no less for being unseen.
        Assert.Empty(hand.ForcedWith);

        hand.TakeSilenceAsAgreement();

        Assert.Equal(8, hand.ForcedWith.Count);
    }
}
