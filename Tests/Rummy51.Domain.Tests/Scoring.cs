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
}
