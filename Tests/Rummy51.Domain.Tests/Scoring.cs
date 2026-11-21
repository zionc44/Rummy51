using static Rummy51.Domain.Tests.Cards;

namespace Rummy51.Domain.Tests;

/// <summary>
/// What a card in somebody's hand is worth, and how a table is placed by
/// what they came to. The doubling and the minus thirty are scored as
/// the hand applies them, and are held to over in <see cref="TheScore"/>.
/// </summary>
public class Scoring
{
    [Fact]
    public void Rule_1_2_a_card_is_worth_its_number()
    {
        Assert.Equal(2, Worth.Held(S(2)));
        Assert.Equal(10, Worth.Held(S(10)));
    }

    [Fact]
    public void Rule_1_2_every_picture_is_worth_ten()
    {
        Assert.Equal(10, Worth.Held(S(Jack)));
        Assert.Equal(10, Worth.Held(S(Queen)));
        Assert.Equal(10, Worth.Held(S(King)));
    }

    [Fact]
    public void Rule_1_2_the_ace_is_eleven() => Assert.Equal(11, Worth.Held(S(Ace)));

    [Fact]
    public void Rule_1_3_a_joker_in_a_hand_is_twenty() => Assert.Equal(20, Worth.Held(Joker()));

    [Fact]
    public void A_hand_is_worth_what_is_in_it() =>
        Assert.Equal(20 + 11 + 10 + 3, Worth.Held([Joker(), S(Ace), H(King), D(3)]));

    [Fact]
    public void An_empty_hand_is_worth_nothing() => Assert.Equal(0, Worth.Held([]));

    [Fact]
    public void Rule_8_1_the_winner_takes_minus_thirty() => Assert.Equal(-30, Worth.Winner);

    [Fact]
    public void Rule_8_1_a_hand_nobody_showed_is_a_hundred_whatever_is_in_it() =>
        Assert.Equal(100, Worth.NeverWentDown);

    // ------------------------------------------------------------ the places

    [Fact]
    public void Rule_8_3_the_lowest_comes_first() =>
        Assert.Equal([1, 2, 3, 4], Placing.Of([-30, 8, 20, 50]));

    [Fact]
    public void Rule_8_3_wherever_they_are_sitting() =>
        Assert.Equal([3, 1, 2], Placing.Of([50, -30, 8]));

    [Fact]
    public void Rule_8_3_two_worth_the_same_take_the_same_place() =>
        Assert.Equal([1, 2, 2, 4], Placing.Of([-30, 18, 18, 24]));

    [Fact]
    public void Rule_8_3_and_the_place_after_them_is_the_one_that_would_have_followed() =>
        Assert.Equal([1, 2, 2, 2], Placing.Of([-30, 18, 18, 18]));

    [Fact]
    public void Rule_8_3_nothing_to_place() => Assert.Empty(Placing.Of([]));

    [Fact]
    public void Rule_9_4_a_seat_that_was_not_in_the_hand_has_no_place_in_it() =>
        Assert.Equal([1, 0, 2], Placing.Of([-30, 0, 40], [true, false, true]));

    [Fact]
    public void Rule_9_4_and_is_not_placed_ahead_of_whoever_played_and_lost()
    {
        // Nought beats forty, so a seat scored nought for not being there
        // would come first if it were placed at all.
        var places = Placing.Of([40, 0, 60], [true, false, true]);

        Assert.Equal(0, places[1]);
        Assert.Equal(1, places[0]);
    }

    // ------------------------------------------------- and where a game did

    /// <summary>Everybody who sat down, which is the usual case.</summary>
    private static IReadOnlyList<bool> All(int seats) => [.. Enumerable.Repeat(true, seats)];

    [Fact]
    public void Rule_9_5_whoever_lasted_longest_comes_first() =>
        Assert.Equal([1, 3, 2], Placing.InTheGame([5, 2, 4], [120, 210, 230], All(3)));

    [Fact]
    public void Rule_9_5_and_a_lower_total_does_not_buy_a_place()
    {
        // Out in the second hand with 205 against out in the fifth with
        // 230. Lasting is the whole of it, so the 230 comes first.
        var places = Placing.InTheGame([5, 2], [230, 205], All(2));

        Assert.Equal([1, 2], places);
    }

    [Fact]
    public void Rule_9_5_two_out_in_the_same_hand_are_split_by_what_they_had() =>
        Assert.Equal([1, 3, 2], Placing.InTheGame([4, 3, 3], [90, 240, 210], All(3)));

    [Fact]
    public void Rule_9_5_two_alike_in_both_share_a_place() =>
        Assert.Equal([1, 2, 2, 4], Placing.InTheGame([6, 3, 3, 1], [80, 220, 220, 260], All(4)));

    [Fact]
    public void Rule_9_5_the_last_one_in_is_first_without_being_put_there()
    {
        // The winner is the only seat under the number and the only one to
        // have played every hand, so both keys have them first already.
        var places = Placing.InTheGame([7, 7, 4], [150, 205, 240], All(3));

        Assert.Equal(1, places[0]);
    }

    [Fact]
    public void Rule_9_6_a_chair_given_up_is_still_placed_by_how_long_it_lasted() =>
        Assert.Equal([2, 0, 1], Placing.InTheGame([3, 0, 5], [220, 0, 140], [true, false, true]));

    [Fact]
    public void Rule_9_2_a_game_of_one_hand_places_by_that_hand() =>
        Assert.Equal([2, 1, 3], Placing.InTheGame([1, 1, 1], [40, -30, 100], All(3)));

    [Fact]
    public void Rule_9_5_nothing_to_place() => Assert.Empty(Placing.InTheGame([], [], []));
}
