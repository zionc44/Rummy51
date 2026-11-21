namespace Rummy51.Domain.Tests;

/// <summary>
/// Who cuts, who is dealt what, and who moves first.
/// </summary>
public class TheDeal
{
    [Fact]
    public void A_hand_begins_waiting_to_be_cut() =>
        Assert.Equal(HandPhase.Cutting, new Hand(2, 2, 0, 1).Phase);

    [Fact]
    public void Rule_2_2_the_cut_is_the_chair_before_the_dealer()
    {
        Assert.Equal(3, new Hand(4, 2, dealerSeat: 0, seed: 1).CutterSeat);
        Assert.Equal(1, new Hand(4, 2, dealerSeat: 2, seed: 1).CutterSeat);
    }

    [Fact]
    public void Rule_2_2_a_deck_is_cut_once_and_never_again()
    {
        var hand = new Hand(2, 2, 0, 1);

        Assert.True(hand.CutAt(20));
        Assert.False(hand.CutAt(30));
        Assert.Equal(20, hand.Deck.CutPosition);
    }

    [Fact]
    public void Rule_2_2_the_card_at_the_cut_is_the_finishing_card()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);

        Assert.NotNull(hand.Deck.FinishingCard);
    }

    [Fact]
    public void Rule_2_4_fifteen_to_the_player_after_the_dealer_and_fourteen_to_the_rest()
    {
        var hand = Dealt.Shuffled(seats: 4);

        Assert.Equal(15, hand.Hands[1].Count);
        Assert.Equal(14, hand.Hands[2].Count);
        Assert.Equal(14, hand.Hands[3].Count);
        Assert.Equal(14, hand.Hands[0].Count);
    }

    [Fact]
    public void Rule_2_4_the_deal_is_shown_pass_by_pass()
    {
        var hand = Dealt.Shuffled(seats: 3);

        Assert.NotEmpty(hand.Passes);
        Assert.Equal(15 + 14 + 14, hand.Passes.Sum(pass => pass.Count));
    }

    [Fact]
    public void Rule_3_1_the_player_after_the_dealer_opens()
    {
        Assert.Equal(1, Dealt.Shuffled(seats: 4, dealerSeat: 0).TurnSeat);
        Assert.Equal(0, Dealt.Shuffled(seats: 4, dealerSeat: 3).TurnSeat);
    }

    [Fact]
    public void Rule_3_1_and_their_fifteenth_card_was_the_draw() =>
        Assert.True(Dealt.Shuffled().HasDrawn);

    [Fact]
    public void A_deck_is_two_packs_and_the_jokers_it_was_opened_with()
    {
        Assert.Equal((2 * 52) + 2, new Hand(2, 2, 0, 1).Deck.Size);
        Assert.Equal((2 * 52) + 4, new Hand(2, 4, 0, 1).Deck.Size);
        Assert.Equal(2 * 52, new Hand(2, 0, 0, 1).Deck.Size);
    }

    [Fact]
    public void Rule_11_5_the_same_seed_deals_the_same_hand()
    {
        var one = Dealt.Shuffled(seed: 77);
        var other = Dealt.Shuffled(seed: 77);

        Assert.Equal(Codes(one, 0), Codes(other, 0));
        Assert.Equal(Codes(one, 1), Codes(other, 1));
    }

    [Fact]
    public void And_a_different_seed_does_not() =>
        Assert.NotEqual(Codes(Dealt.Shuffled(seed: 77), 0), Codes(Dealt.Shuffled(seed: 78), 0));

    [Fact]
    public void The_laid_out_deck_deals_what_it_was_laid_out_to_deal()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);

        Assert.Equal(
            Dealt.AWinningHand.Select(card => card.Code),
            hand.Hands[hand.TurnSeat].Select(held => held.Card.Code));
    }

    private static IEnumerable<string> Codes(Hand hand, int seat) =>
        hand.Hands[seat].Select(held => held.Card.Code);
}
