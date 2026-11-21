using static Rummy51.Domain.Tests.Cards;

namespace Rummy51.Domain.Tests;

/// <summary>
/// What a run and a set are, and what one is worth. Every example here is
/// one the rules give in their own words, because where a rule names a hand
/// and a number there is nothing better to hold the code to.
/// </summary>
public class RunsAndSets
{
    [Fact]
    public void Rule_4_1_three_of_the_same_suit_in_order_is_a_run() =>
        Assert.True(Melding.IsMeld([S(4), S(5), S(6)]));

    [Fact]
    public void Rule_4_1_three_of_the_same_rank_is_a_set() =>
        Assert.True(Melding.IsMeld([S(8), H(8), D(8)]));

    [Fact]
    public void Rule_4_1_four_of_a_rank_is_a_set_and_five_is_nothing()
    {
        Assert.True(Melding.IsMeld([S(8), H(8), D(8), C(8)]));
        Assert.False(Melding.IsMeld([S(8), H(8), D(8), C(8), Joker()]));
    }

    [Fact]
    public void Rule_4_1_a_set_may_not_hold_the_same_card_twice() =>
        Assert.False(Melding.IsMeld([S(8), H(8), H(8)]));

    [Fact]
    public void Rule_4_1_two_is_not_enough_for_either() =>
        Assert.False(Melding.IsMeld([S(4), S(5)]));

    [Fact]
    public void Rule_4_1_a_run_is_of_one_suit() =>
        Assert.False(Melding.IsMeld([S(4), H(5), S(6)]));

    [Fact]
    public void Rule_4_2_Q_K_A_is_a_run() =>
        Assert.True(Melding.IsMeld([S(Queen), S(King), S(Ace)]));

    [Fact]
    public void Rule_4_2_A_2_3_is_a_run() =>
        Assert.True(Melding.IsMeld([S(Ace), S(2), S(3)]));

    [Fact]
    public void Rule_4_2_K_A_2_is_not_because_the_ace_does_not_turn_the_corner() =>
        Assert.False(Melding.IsMeld([S(King), S(Ace), S(2)]));

    [Fact]
    public void Rule_4_2_a_run_reads_either_way_round() =>
        Assert.True(Melding.IsMeld([S(6), S(5), S(4)]));

    // ------------------------------------------------- what a joker stands for

    [Fact]
    public void Rule_5_3_a_joker_in_a_run_counts_as_the_card_it_stands_in_for() =>
        Assert.Equal(4 + 5 + 6 + 7, Melding.WorthOnTheTable([S(4), S(5), Joker(), S(7)]));

    [Fact]
    public void Rule_5_4_nine_ten_joker_queen_is_thirty_nine() =>
        Assert.Equal(39, Melding.WorthOnTheTable([S(9), S(10), Joker(), S(Queen)]));

    [Fact]
    public void Rule_5_5_seven_seven_joker_is_twenty_one() =>
        Assert.Equal(21, Melding.WorthOnTheTable([S(7), H(7), Joker()]));

    [Fact]
    public void Rule_1_2_the_ace_is_eleven_wherever_it_is_read()
    {
        Assert.Equal(11 + 2 + 3, Melding.WorthOnTheTable([S(Ace), S(2), S(3)]));
        Assert.Equal(10 + 10 + 11, Melding.WorthOnTheTable([S(Queen), S(King), S(Ace)]));
    }

    [Fact]
    public void Rule_1_2_every_picture_is_worth_ten() =>
        Assert.Equal(30, Melding.WorthOnTheTable([S(Jack), S(Queen), S(King)]));

    [Fact]
    public void What_is_not_a_meld_is_worth_nothing() =>
        Assert.Equal(0, Melding.WorthOnTheTable([S(4), H(9), D(2)]));

    // ------------------------------------------------------- the clean three

    [Fact]
    public void Rule_5_2_six_seven_eight_joker_ten_holds_a_clean_three() =>
        Assert.True(Melding.HasCleanRun([S(6), S(7), S(8), Joker(), S(10)]));

    [Fact]
    public void Rule_5_2_five_six_joker_eight_nine_does_not() =>
        Assert.False(Melding.HasCleanRun([S(5), S(6), Joker(), S(8), S(9)]));

    [Fact]
    public void Rule_5_1_a_set_is_never_the_clean_three() =>
        Assert.False(Melding.HasCleanRun([S(8), H(8), D(8)]));

    // --------------------------------------------- fifty-one, and what it costs

    [Fact]
    public void Rule_5_1_fifty_one_is_not_enough_without_a_clean_run() =>
        Assert.False(Melding.IsAShowing([[S(King), H(King), D(King)], [S(10), H(10), D(10)]]));

    [Fact]
    public void Rule_5_1_a_clean_run_is_not_enough_without_fifty_one() =>
        Assert.False(Melding.IsAShowing([[S(4), S(5), S(6)], [S(8), H(8), D(8)]]));

    [Fact]
    public void Rule_5_1_both_together_are_a_showing() =>
        Assert.True(Melding.IsAShowing(
            [[S(4), S(5), S(6)], [S(Ace), H(Ace), D(Ace), C(Ace)]]));

    [Fact]
    public void Rule_5_3_a_clean_run_costs_nothing_to_provide()
    {
        // All of it is the clean part, so there is nothing to take off it.
        Assert.Equal(15 + 39, Melding.ShowingWorth(
            [[S(4), S(5), S(6)], [S(9), S(10), Joker(), S(Queen)]]));
    }

    [Fact]
    public void Rule_5_3_only_the_clean_part_of_the_providing_run_counts()
    {
        // Four to eight is thirty with the joker standing for the seven, and
        // the three it provides are worth fifteen. Fifteen is what is left.
        Assert.Equal(15, Melding.ShowingWorth([[S(4), S(5), S(6), Joker(), S(8)]]));
    }

    [Fact]
    public void Rule_5_4_a_run_with_no_clean_three_is_worth_all_of_itself() =>
        Assert.Equal(39, Melding.ShowingWorth([[S(9), S(10), Joker(), S(Queen)]]));

    [Fact]
    public void Rule_5_3_the_cheapest_run_to_give_up_is_the_one_that_provides()
    {
        // Two runs that could provide. The spades cost fifteen to give up,
        // being clean only as far as the joker; the hearts cost nothing,
        // being clean end to end. Nothing is what is given up.
        Assert.Equal(
            30 + 29,
            Melding.ShowingWorth([[S(4), S(5), S(6), Joker(), S(8)], [H(9), H(10), H(Jack)]]));
    }

    // ------------------------------------------------ the book's own three

    // Rule 5.3 and 5.4 work three examples out in words. They are asserted
    // here and again, card for card, in melds.spec.ts across the wire: the
    // twins are only checkable while both are answering the same question.

    [Fact]
    public void Rule_5_3_six_to_ten_with_three_tens_comes_to_exactly_fifty_one() =>
        Assert.Equal(
            51,
            Melding.ShowingWorth(
                [[S(6), S(7), S(8), Joker(), S(10)], [S(10), H(10), D(10)]]));

    [Fact]
    public void Rule_5_3_five_to_nine_laid_alone_is_the_clean_three_of_it() =>
        Assert.Equal(18, Melding.ShowingWorth([[H(5), H(6), H(7), Joker(), H(9)]]));

    [Fact]
    public void Rule_5_3_and_beside_a_clean_run_of_its_own_is_all_of_itself() =>
        Assert.Equal(
            35 + 15,
            Melding.ShowingWorth([[H(5), H(6), H(7), Joker(), H(9)], [D(4), D(5), D(6)]]));
}
