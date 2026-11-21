namespace Rummy51.Domain.Tests;

/// <summary>
/// Rule 3.5: what happens when the last card comes off the stock. Nobody
/// reaches it by playing a hand out by hand, which is the whole reason it
/// went unenforced for as long as it did - here it is sixty-odd turns and
/// a tenth of a second.
/// </summary>
public class TheStock
{
    /// <summary>
    /// Played out until something stops it. Every turn drawn and thrown is
    /// one card off the stock and one onto the pile, and nobody lays
    /// anything, so the hand cannot end before the stock does.
    /// </summary>
    private static Hand PlayedDry(int seats = 3)
    {
        var hand = Dealt.Shuffled(seats: seats);

        for (var turn = 0; turn < 500 && hand.Phase == HandPhase.Playing; turn++)
        {
            hand.PlayOutTheTurn(hand.TurnSeat);
        }

        return hand;
    }

    [Fact]
    public void Rule_3_5_the_hand_stops_when_the_stock_runs_out()
    {
        var hand = PlayedDry();

        Assert.Equal(HandPhase.Restocking, hand.Phase);
        Assert.Empty(hand.Board.Stock);
    }

    [Fact]
    public void Rule_3_5_and_nobody_moves_until_the_table_has_seen_it()
    {
        var hand = PlayedDry();
        var seat = hand.TurnSeat;

        Assert.False(hand.Draw(seat, DrawnFrom.Discards));
        Assert.False(hand.Discard(seat, hand.Hands[seat][0].Id));
    }

    [Fact]
    public void Rule_3_5_the_pile_becomes_the_stock_once_everybody_has()
    {
        var hand = PlayedDry();
        var thrown = hand.Board.Discards.Count;

        SeenByAll(hand);

        Assert.Equal(HandPhase.Playing, hand.Phase);
        Assert.Equal(thrown - 1, hand.Board.Stock.Count);
    }

    [Fact]
    public void Rule_3_5_and_the_card_last_thrown_stays_where_it_lies()
    {
        var hand = PlayedDry();
        var top = hand.Board.TopOfDiscards;

        SeenByAll(hand);

        Assert.Single(hand.Board.Discards);
        Assert.Equal(top?.Code, hand.Board.TopOfDiscards?.Code);
    }

    [Fact]
    public void Rule_3_3_so_the_pile_is_never_one_nobody_can_draw_from()
    {
        var hand = PlayedDry();

        SeenByAll(hand);

        var seat = hand.TurnSeat;

        Assert.True(hand.Draw(seat, DrawnFrom.Discards));
    }

    [Fact]
    public void Rule_3_5_one_seat_saying_so_is_not_the_table()
    {
        var hand = PlayedDry();

        Assert.True(hand.SeeTheRestock(hand.TurnSeat));
        Assert.Equal(HandPhase.Restocking, hand.Phase);
    }

    [Fact]
    public void Rule_3_5_and_the_clock_does_it_for_whoever_has_not()
    {
        var hand = PlayedDry();

        Assert.True(hand.RestockForWhoeverHasNot());
        Assert.Equal(HandPhase.Playing, hand.Phase);
        Assert.NotEmpty(hand.Board.Stock);
    }

    [Fact]
    public void Rule_11_5_the_order_it_came_out_in_is_written_down()
    {
        var hand = PlayedDry();

        SeenByAll(hand);

        var written = Assert.Single(hand.Restocks);

        Assert.Equal(hand.Board.Stock.Count, written.Split(',').Length);
        Assert.Equal(string.Join(',', hand.Board.Stock.Select(card => card.Code)), written);
    }

    [Fact]
    public void Rule_9_4_a_seat_that_is_out_does_not_hold_it_up()
    {
        var hand = new Hand(3, 2, 0, seed: 9, [true, false, true]);

        hand.CutAt(11);
        hand.Deal();
        hand.Begin();

        for (var turn = 0; turn < 500 && hand.Phase == HandPhase.Playing; turn++)
        {
            hand.PlayOutTheTurn(hand.TurnSeat);
        }

        Assert.Equal(HandPhase.Restocking, hand.Phase);
        Assert.False(hand.SeeTheRestock(1));

        hand.SeeTheRestock(0);
        hand.SeeTheRestock(2);

        Assert.Equal(HandPhase.Playing, hand.Phase);
    }

    [Fact]
    public void Rule_3_5_the_turn_it_emptied_is_not_interrupted()
    {
        var hand = Dealt.Shuffled(seats: 3);

        for (var turn = 0; turn < 500 && hand.Board.Stock.Count > 1; turn++)
        {
            hand.PlayOutTheTurn(hand.TurnSeat);
        }

        var seat = hand.TurnSeat;

        // One card left, so this draw takes the last of it - and the turn
        // goes on being theirs until they have thrown.
        Assert.True(hand.Draw(seat, DrawnFrom.Stock));
        Assert.Empty(hand.Board.Stock);
        Assert.Equal(HandPhase.Playing, hand.Phase);

        hand.Discard(seat, hand.Hands[seat][0].Id);

        Assert.Equal(HandPhase.Restocking, hand.Phase);
    }

    private static void SeenByAll(Hand hand)
    {
        foreach (var seat in Enumerable.Range(0, hand.Seats))
        {
            hand.SeeTheRestock(seat);
        }
    }
}
