namespace Rummy51.Domain.Tests;

/// <summary>
/// A turn, and the three questions it is not allowed to end on:
/// what it owes back, what it has shown, and whether it may go out at all.
/// </summary>
public class TheTurn
{
    [Fact]
    public void Rule_3_2_a_throw_hands_the_turn_on()
    {
        var hand = Dealt.Shuffled(seats: 3);
        var seat = hand.TurnSeat;

        hand.Discard(seat, hand.Hands[seat][0].Id);

        Assert.Equal((seat + 1) % 3, hand.TurnSeat);
    }

    [Fact]
    public void Rule_3_2_and_goes_round_the_table()
    {
        var hand = Dealt.Shuffled(seats: 3);
        var seats = new List<int>();

        foreach (var _ in Enumerable.Range(0, 4))
        {
            seats.Add(hand.TurnSeat);
            hand.PlayOutTheTurn(hand.TurnSeat);
        }

        Assert.Equal([1, 2, 0, 1], seats);
    }

    [Fact]
    public void Nobody_moves_out_of_turn()
    {
        var hand = Dealt.Shuffled(seats: 3);
        var notTheirs = (hand.TurnSeat + 1) % 3;

        Assert.False(hand.Discard(notTheirs, hand.Hands[notTheirs][0].Id));
        Assert.False(hand.Draw(notTheirs, DrawnFrom.Stock));
    }

    [Fact]
    public void Rule_3_1_the_opener_has_drawn_and_may_not_draw_again() =>
        Assert.False(Dealt.Shuffled().Draw(Dealt.Shuffled().TurnSeat, DrawnFrom.Stock));

    [Fact]
    public void Rule_3_3_a_card_is_taken_and_a_card_is_thrown()
    {
        var hand = Dealt.Shuffled(seats: 3);

        hand.PlayOutTheTurn(hand.TurnSeat);

        var seat = hand.TurnSeat;

        Assert.False(hand.HasDrawn);
        Assert.True(hand.Draw(seat, DrawnFrom.Stock));
        Assert.True(hand.HasDrawn);
        Assert.Equal(15, hand.Hands[seat].Count);
    }

    [Fact]
    public void Rule_3_4_a_card_off_the_pile_is_owed_back_to_the_table()
    {
        var hand = Dealt.Shuffled(seats: 3);

        hand.PlayOutTheTurn(hand.TurnSeat);

        var seat = hand.TurnSeat;

        Assert.True(hand.Draw(seat, DrawnFrom.Discards));
        Assert.NotEmpty(hand.StillOwed(seat));
        Assert.False(hand.HasPutBackWhatItTook(seat));
        Assert.False(hand.CanEndTheTurn(seat));
    }

    [Fact]
    public void Rule_3_4_a_card_off_the_stock_owes_nothing()
    {
        var hand = Dealt.Shuffled(seats: 3);

        hand.PlayOutTheTurn(hand.TurnSeat);

        var seat = hand.TurnSeat;

        hand.Draw(seat, DrawnFrom.Stock);

        Assert.Empty(hand.StillOwed(seat));
        Assert.True(hand.CanEndTheTurn(seat));
    }

    [Fact]
    public void Rule_6_4_nobody_may_go_out_in_the_first_round()
    {
        var hand = Dealt.Shuffled(seats: 3);

        Assert.False(hand.CanFinish);

        hand.PlayOutTheTurn(hand.TurnSeat);
        Assert.False(hand.CanFinish);

        hand.PlayOutTheTurn(hand.TurnSeat);
        Assert.True(hand.CanFinish);
    }

    [Fact]
    public void Rule_6_5_a_hand_keeps_two_back_while_going_out_is_closed()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        // Fourteen of the fifteen are melds, and laying them all would leave
        // one card and a throw that would be going out.
        Assert.False(hand.CanPartWith(seat, 14));
        Assert.True(hand.CanPartWith(seat, 13));
    }

    [Fact]
    public void Rule_5_6_a_turn_may_not_end_on_a_showing_that_is_not_one()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        // The kings alone are thirty, which is not fifty-one, and they are
        // not a run either.
        Assert.True(hand.LayDown(seat, Ids(hand, seat, 8, 9, 10)));
        Assert.False(hand.CanEndTheTurn(seat));
    }

    [Fact]
    public void Rule_5_8_the_right_arrives_with_the_showing_and_not_with_the_throw()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        // Taken before anything is laid: laying shortens the hand, and the
        // places left in it are not the places they were.
        var cards = Held(hand, seat);

        hand.LayDown(seat, [.. cards[..4]]);
        hand.LayDown(seat, [.. cards[4..8]]);

        // This is the whole of 5.8: the right is theirs now, before any
        // throw has made it final, which is what lets them go down and add
        // to the table in the one turn.
        Assert.True(hand.CanAddToTheTable(seat));
    }

    [Fact]
    public void Rule_11_3_and_the_table_is_told_the_moment_it_stands()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;
        var cards = Held(hand, seat);

        hand.LayDown(seat, [.. cards[..4]]);
        Assert.False(hand.WentDown[seat]);

        hand.LayDown(seat, [.. cards[4..8]]);
        Assert.True(hand.WentDown[seat]);

        hand.Undo(seat);
        Assert.False(hand.WentDown[seat]);
    }

    [Fact]
    public void Rule_5_8_and_the_throw_is_what_settles_it()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;
        var cards = Held(hand, seat);

        hand.LayDown(seat, [.. cards[..4]]);
        hand.LayDown(seat, [.. cards[4..8]]);
        hand.Discard(seat, cards[^1]);

        // Nothing this turn is holding it up any more - the turn is not even
        // theirs - so what answers now is what the throw wrote down.
        Assert.NotEqual(seat, hand.TurnSeat);
        Assert.True(hand.WentDown[seat]);
    }

    [Fact]
    public void Rule_5_8_and_is_lost_again_if_part_of_the_showing_goes_back()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        var cards = Held(hand, seat);

        hand.LayDown(seat, [.. cards[..4]]);
        hand.LayDown(seat, [.. cards[4..8]]);
        Assert.True(hand.CanAddToTheTable(seat));

        hand.Undo(seat);

        Assert.False(hand.CanAddToTheTable(seat));

        // Undoing takes the whole turn back and not one step of it, so what
        // is left is a turn that has laid nothing - and rule 5.6 allows a
        // turn to end on nothing.
        Assert.True(hand.CanEndTheTurn(seat));
    }

    [Fact]
    public void Rule_5_7_only_a_player_who_is_down_may_add_to_the_table()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        var cards = Held(hand, seat);

        hand.LayDown(seat, [.. cards[..4]]);
        Assert.False(hand.CanAddToTheTable(seat));

        hand.LayDown(seat, [.. cards[4..8]]);
        Assert.True(hand.CanAddToTheTable(seat));
    }

    [Fact]
    public void Rule_4_1_what_is_not_a_meld_is_refused_as_it_is_laid()
    {
        var hand = Dealt.Holding(Dealt.AWinningHand);
        var seat = hand.TurnSeat;

        // The four of spades, the nine of hearts and the king of clubs.
        Assert.False(hand.LayDown(seat, Ids(hand, seat, 0, 4, 8)));
    }

    /// <summary>Cards by where they sit in the hand as it stands.</summary>
    internal static int[] Ids(Hand hand, int seat, params int[] at) =>
        [.. at.Select(one => hand.Hands[seat][one].Id)];

    /// <summary>The whole hand by its cards, before any of it is laid.</summary>
    internal static int[] Held(Hand hand, int seat) =>
        [.. hand.Hands[seat].Select(held => held.Id)];
}
