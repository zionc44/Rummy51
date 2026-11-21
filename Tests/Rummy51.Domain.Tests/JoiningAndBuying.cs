using static Rummy51.Domain.Tests.Cards;

namespace Rummy51.Domain.Tests;

/// <summary>
/// Rules 5.9 to 5.11: what may be put on what is already lying there, and
/// what may be taken off it. Both are asked the same way - the card is put
/// in the place and whatever is still a meld afterwards is an answer.
/// </summary>
public class JoiningAndBuying
{
    [Fact]
    public void Rule_5_9_a_card_joins_a_run_at_either_end()
    {
        Assert.Equal([MeldEnd.Front], Melding.WhereItFits([S(5), S(6), S(7)], S(4)));
        Assert.Equal([MeldEnd.Back], Melding.WhereItFits([S(5), S(6), S(7)], S(8)));
    }

    [Fact]
    public void Rule_5_9_and_nowhere_else() =>
        Assert.Empty(Melding.WhereItFits([S(5), S(6), S(7)], S(10)));

    [Fact]
    public void Rule_5_9_it_has_to_carry_the_suit_on() =>
        Assert.Empty(Melding.WhereItFits([S(5), S(6), S(7)], H(8)));

    [Fact]
    public void Rule_5_9_nothing_goes_above_the_ace_of_Q_K_A() =>
        Assert.Empty(Melding.WhereItFits([S(Queen), S(King), S(Ace)], S(2)));

    [Fact]
    public void Rule_5_9_nothing_goes_below_the_ace_of_A_2_3() =>
        Assert.Empty(Melding.WhereItFits([S(Ace), S(2), S(3)], S(King)));

    [Fact]
    public void Rule_5_9_a_joker_joins_wherever_a_card_could() =>
        Assert.Equal(
            [MeldEnd.Front, MeldEnd.Back],
            Melding.WhereItFits([S(5), S(6), S(7)], Joker()));

    [Fact]
    public void Rule_5_10_a_card_joins_a_set_of_its_rank_in_a_suit_it_has_not_got() =>
        Assert.Equal([MeldEnd.Back], Melding.WhereItFits([S(8), H(8), D(8)], C(8)));

    [Fact]
    public void Rule_5_10_and_not_in_a_suit_it_has() =>
        Assert.Empty(Melding.WhereItFits([S(8), H(8), D(8)], H(8)));

    [Fact]
    public void Rule_5_10_only_while_the_set_is_three() =>
        Assert.Empty(Melding.WhereItFits([S(8), H(8), D(8), C(8)], Joker()));

    [Fact]
    public void Rule_5_10_a_set_has_no_ends() =>
        Assert.Single(Melding.WhereItFits([S(8), H(8), D(8)], C(8)));

    [Fact]
    public void Rule_4_1_nothing_joins_a_pair_because_a_pair_is_not_a_meld() =>
        Assert.Empty(Melding.WhereItFits([S(5), S(6)], S(7)));

    // ------------------------------------------------- buying a joker out

    [Fact]
    public void Rule_5_11_a_run_names_the_card_its_joker_stands_for_exactly()
    {
        Assert.True(Melding.CanReplace([S(5), S(6), Joker(), S(8)], 2, S(7)));
        Assert.False(Melding.CanReplace([S(5), S(6), Joker(), S(8)], 2, S(9)));
        Assert.False(Melding.CanReplace([S(5), S(6), Joker(), S(8)], 2, H(7)));
    }

    [Fact]
    public void Rule_5_11_a_set_names_the_rank_and_leaves_the_suit()
    {
        Assert.True(Melding.CanReplace([S(8), H(8), D(8), Joker()], 3, C(8)));
        Assert.False(Melding.CanReplace([S(8), H(8), D(8), Joker()], 3, C(9)));
    }

    [Fact]
    public void Rule_5_11_and_not_a_suit_the_set_already_has() =>
        Assert.False(Melding.CanReplace([S(8), H(8), D(8), Joker()], 3, H(8)));

    [Fact]
    public void Rule_5_11_a_set_gives_its_joker_up_only_once_it_is_four() =>
        Assert.False(Melding.CanReplace([S(8), H(8), Joker()], 2, D(8)));

    [Fact]
    public void Rule_5_11_a_joker_for_a_joker_is_not_a_card_coming_off_the_table() =>
        Assert.False(Melding.CanReplace([S(5), S(6), Joker(), S(8)], 2, Joker(2)));

    [Fact]
    public void Rule_5_11_there_has_to_be_a_joker_in_that_place() =>
        Assert.False(Melding.CanReplace([S(5), S(6), S(7)], 1, S(6)));

    [Fact]
    public void A_place_that_is_not_in_the_meld_names_nothing()
    {
        Assert.False(Melding.CanReplace([S(5), S(6), Joker()], -1, S(7)));
        Assert.False(Melding.CanReplace([S(5), S(6), Joker()], 3, S(7)));
    }
}
