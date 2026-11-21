using Rummy51.Domain.Entities;

namespace Rummy51.Domain.Tests;

/// <summary>
/// A redeal offered, answered, read and then dealt. The clock
/// belongs to the table, so the times here are given rather than taken -
/// which is also what lets a test say exactly when something was asked.
/// </summary>
public class TheRedeal
{
    private static readonly DateTimeOffset Asked = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Settled = Asked.AddSeconds(9);

    [Fact]
    public void Rule_12_1_it_is_offered_on_their_turn_and_after_their_card()
    {
        var hand = Dealt.Shuffled(seats: 3);
        var seat = hand.TurnSeat;

        Assert.True(hand.CanOfferARedeal(seat));
        Assert.False(hand.CanOfferARedeal((seat + 1) % 3));
    }

    [Fact]
    public void Rule_12_1_and_not_once_anything_has_been_put_on_the_table()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        hand.LayDown(seat, [.. TheTurn.Held(hand, seat)[..4]]);

        Assert.False(hand.CanOfferARedeal(seat));
    }

    [Fact]
    public void Rule_12_2_once_in_a_hand()
    {
        var hand = Offered(out var by);

        hand.Answer(Other(by), agreed: false);
        hand.StopShowingTheResult(Settled);

        Assert.False(hand.CanOfferARedeal(by));
    }

    [Fact]
    public void Rule_12_3_one_refusal_and_nothing_happens_at_all()
    {
        var hand = Offered(out var by);

        hand.Answer(Other(by), agreed: false);
        hand.StopShowingTheResult(Settled);

        Assert.Equal(HandPhase.Playing, hand.Phase);
        Assert.Equal(0, hand.AgreedRedeals);
        Assert.Equal(by, hand.TurnSeat);
    }

    [Fact]
    public void Rule_12_3_everybody_agreeing_deals_it_again()
    {
        var hand = Offered(out var by);

        hand.Answer(Other(by), agreed: true);
        hand.StopShowingTheResult(Settled);

        Assert.Equal(HandPhase.Keeping, hand.Phase);
        Assert.Equal(1, hand.AgreedRedeals);
    }

    [Fact]
    public void Rule_12_4_silence_is_agreement()
    {
        var hand = Offered(out _);

        hand.TakeSilenceAsAgreement();
        hand.StopShowingTheResult(Settled);

        Assert.Equal(HandPhase.Keeping, hand.Phase);
    }

    [Fact]
    public void Rule_12_4_and_is_kept_apart_from_one_in_what_is_written_down()
    {
        var hand = Offered(out var by);

        hand.TakeSilenceAsAgreement();
        hand.StopShowingTheResult(Settled);

        var redeal = hand.Redeals.Single();

        Assert.Equal(RedealReply.Offered, redeal.Replies[by]);
        Assert.Equal(RedealReply.Silent, redeal.Replies[Other(by)]);
    }

    [Fact]
    public void Rule_12_4_no_answer_is_shown_to_anybody_who_has_not_given_one()
    {
        // Three seats, because masking needs somebody to mask from: with
        // two there is nobody whose answer is not already yours or theirs.
        var hand = Dealt.Shuffled(seats: 3);
        var by = hand.TurnSeat;

        hand.OfferARedeal(by, Asked);

        var watching = (by + 1) % 3;
        var third = (by + 2) % 3;

        hand.Answer(third, agreed: false);

        // The offerer's is the one answer anybody may see: it was given
        // before the question existed. The third one's is not.
        var seen = hand.OfferAsSeenBy(watching);

        Assert.True(seen[by]);
        Assert.Null(seen[third]);

        hand.Answer(watching, agreed: true);

        Assert.False(hand.OfferAsSeenBy(watching)[third]);
    }

    [Fact]
    public void Rule_12_6_everybody_keeps_exactly_three()
    {
        var hand = Agreed();
        var seat = 0;
        var three = hand.Hands[seat].Take(3).Select(held => held.Id).ToArray();

        Assert.False(hand.Keep(seat, [.. three.Take(2)]));
        Assert.False(hand.Keep(seat, [three[0], three[0], three[1]]));
        Assert.True(hand.Keep(seat, three));
    }

    [Fact]
    public void Rule_12_6_and_three_of_their_own()
    {
        var hand = Agreed();
        var somebody_elses = hand.Hands[1].Take(3).Select(held => held.Id).ToArray();

        Assert.False(hand.Keep(0, somebody_elses));
    }

    [Fact]
    public void Rule_12_7_it_is_cut_again_and_dealt_one_round_shorter()
    {
        var hand = Agreed();

        hand.KeepForWhoeverHasNot();

        Assert.Equal(HandPhase.Cutting, hand.Phase);

        hand.CutAt(11);
        hand.Deal();

        // Rule 12.7: twelve and eleven on top of the three each kept, so
        // every hand is the size it was.
        Assert.Equal(15, hand.Hands[hand.DealerSeat == 0 ? 1 : 0].Count);
        Assert.Equal(14, hand.Hands[hand.DealerSeat].Count);
    }

    [Fact]
    public void Rule_12_8_the_deal_does_not_move_on()
    {
        var hand = Agreed();
        var dealer = hand.DealerSeat;

        hand.KeepForWhoeverHasNot();

        Assert.Equal(dealer, hand.DealerSeat);
    }

    [Fact]
    public void Rule_12_2_and_the_right_comes_back_with_the_deal()
    {
        var hand = Agreed();

        hand.KeepForWhoeverHasNot();
        hand.CutAt(11);
        hand.Deal();
        hand.Begin();

        Assert.True(hand.CanOfferARedeal(hand.TurnSeat));
    }

    [Fact]
    public void Rule_12_2_and_what_was_asked_before_is_still_written_down()
    {
        var hand = Agreed();

        hand.KeepForWhoeverHasNot();
        hand.CutAt(11);
        hand.Deal();
        hand.Begin();

        Assert.Single(hand.Redeals);
        Assert.Equal(Asked, hand.Redeals[0].AskedAt);
    }

    [Fact]
    public void Rule_8_2_a_redeal_the_table_agreed_to_doubles_the_hand()
    {
        var hand = Agreed();

        hand.KeepForWhoeverHasNot();

        Assert.Equal(1, hand.AgreedRedeals);
        Assert.Equal(0, hand.ForcedRedeals);
    }

    [Fact]
    public void Nothing_is_written_down_while_the_question_is_still_open()
    {
        var hand = Offered(out _);

        Assert.Empty(hand.Redeals);
    }

    // ------------------------------------------------------------------------

    private static Hand Offered(out int by)
    {
        var hand = Dealt.Shuffled();

        by = hand.TurnSeat;
        hand.OfferARedeal(by, Asked);

        return hand;
    }

    private static Hand Agreed()
    {
        var hand = Offered(out var by);

        hand.Answer(Other(by), agreed: true);
        hand.StopShowingTheResult(Settled);

        return hand;
    }

    private static int Other(int seat) => 1 - seat;
}
