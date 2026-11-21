namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Rule 3.6: what the table does for whoever let their wait run out. Every
/// stage of a hand is somebody taking their time over something, and this
/// is the one place that decides nobody may take for ever.
/// </summary>
public class TheTableActs
{
    [Fact]
    public void Nothing_happens_while_the_wait_is_still_running()
    {
        var table = Opened.Filled(seats: 3);
        var swept = table.Tick(table.Turns.CutSeconds - 1);

        Assert.Empty(swept.Acted);
        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    /// <summary>There is no wrong place to cut, so chance cuts it.</summary>
    [Fact]
    public void Rule_2_2_a_cut_nobody_made_is_made_for_them()
    {
        var table = Opened.Filled(seats: 3);
        var swept = table.Tick(table.Turns.CutSeconds);

        Assert.Equal([table.Id], swept.Acted);
        Assert.Equal(HandPhase.Dealing, table.Hand.Phase);
        Assert.NotNull(table.Hand.CutAt);
    }

    /// <summary>
    /// Not a wait for anybody: the cards went out the moment the deck was
    /// cut, and the screen takes a few seconds over what the server did at
    /// once.
    /// </summary>
    [Fact]
    public void And_the_deal_hands_the_first_turn_on_by_itself()
    {
        var table = Opened.Filled(seats: 3).Cut();

        table.Tick(table.Turns.DealSeconds);

        Assert.Equal(HandPhase.Playing, table.Hand.Phase);
        Assert.NotEqual(-1, table.Hand.TurnSeat);
    }

    /// <summary>
    /// The first player has a whole hand to take in before they throw, which
    /// is more than any later turn asks.
    /// </summary>
    [Fact]
    public void The_first_turn_is_given_longer_than_the_rest()
    {
        var table = Opened.Filled(seats: 3).Dealt();

        Assert.Equal(table.Turns.FirstTurnSeconds, table.Hand.Waiting!.Seconds);

        table.PlayOut();

        Assert.Equal(table.Turns.TurnSeconds, table.Hand.Waiting!.Seconds);
    }

    [Fact]
    public void Rule_3_6_a_turn_nobody_took_is_played_and_handed_on()
    {
        var table = Opened.Filled(seats: 3).Dealt();
        var seat = table.Hand.TurnSeat;

        var swept = table.Tick(table.Turns.FirstTurnSeconds);

        Assert.Equal([table.Id], swept.Acted);
        Assert.NotEqual(seat, table.Hand.TurnSeat);
    }

    /// <summary>
    /// However that went, the table is no longer waiting for the move it
    /// was waiting for. A wait left standing is asked again every tick, for
    /// ever, which is four players' worth of turns in one sweep.
    /// </summary>
    [Fact]
    public void Rule_3_6_and_the_wait_it_acted_on_is_not_left_standing()
    {
        var table = Opened.Filled(seats: 3).Dealt();

        table.Tick(table.Turns.FirstTurnSeconds);

        var seat = table.Hand.TurnSeat;

        Assert.Empty(table.Tick(1).Acted);
        Assert.Equal(seat, table.Hand.TurnSeat);

        Assert.Equal([table.Id], table.Tick(table.Turns.TurnSeconds).Acted);
    }

    /// <summary>
    /// Rule 3.5. Nobody reaches this by playing a hand out by hand, which is
    /// why it went unenforced for as long as it did: four windows left idle
    /// take a turn every few seconds, and fifty cards are gone in minutes.
    /// </summary>
    [Fact]
    public void Rule_3_5_a_stock_played_dry_is_filled_from_the_pile()
    {
        var table = Opened.Filled(seats: 3).Dealt().PlayedDry();

        Assert.Equal(HandPhase.Restocking, table.Hand.Phase);
        Assert.Equal(0, table.Hand.StockSize);

        table.Tick(table.Turns.RestockSeconds);

        Assert.Equal(HandPhase.Playing, table.Hand.Phase);
        Assert.NotEqual(0, table.Hand.StockSize);
    }

    /// <summary>Rule 3.5: the card last thrown stays where it lies.</summary>
    [Fact]
    public void Rule_3_5_and_the_pile_is_never_one_nobody_can_draw_from()
    {
        var table = Opened.Filled(seats: 3).Dealt().PlayedDry();
        var top = table.Hand.DiscardTop;

        table.Tick(table.Turns.RestockSeconds);

        Assert.Equal(1, table.Hand.DiscardCount);
        Assert.Equal(top, table.Hand.DiscardTop);
    }

    /// <summary>
    /// There is nothing to decide here, so the wait is a reading and not a
    /// turn. Both throw paths used to leave a turn's length standing: they
    /// set the wait without asking what the hand had become.
    /// </summary>
    [Fact]
    public void Rule_3_5_and_the_table_is_given_a_reading_and_not_a_turn()
    {
        var turns = new TurnSettings { RestockSeconds = 3, TurnSeconds = 17 };
        var table = Opened.Filled(seats: 3, turns: turns).Dealt().PlayedDry();

        Assert.Equal(3, table.Hand.Waiting!.Seconds);
    }

    /// <summary>Rule 12.4: the table says nothing, and nothing is agreement.</summary>
    [Fact]
    public void Rule_12_4_an_offer_nobody_answered_is_taken_as_agreed()
    {
        var table = Opened.Filled(seats: 3).Dealt();

        Assert.True(table.Hand.CanOfferRedeal);

        table.Tables.OfferARedeal(table.Id, table.Who(table.Hand.TurnSeat));

        Assert.Equal(HandPhase.Offering, table.Hand.Phase);
        Assert.Equal(-1, table.Hand.Waiting!.Seat);

        table.Tick(table.Turns.OfferSeconds);

        Assert.NotEqual(HandPhase.Offering, table.Hand.Phase);
    }

    /// <summary>
    /// A table that has been emptied by rule 10.4 is one nobody is at, and
    /// the lobby has to be told its row is gone.
    /// </summary>
    [Fact]
    public void Rule_10_7_a_sweep_forgets_a_table_nobody_is_at()
    {
        var table = Opened.Filled(seats: 2).Dealt();

        table.Away(0);
        table.Away(1);

        Assert.Null(table.Tables.Find(table.Id));
        Assert.Empty(table.Tables.All());
    }
}
