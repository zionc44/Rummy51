namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Chapter 10, which makes two questions one: who is sitting here, and who
/// is actually looking at it. A seat before the cards go out is just a
/// chair; once they have, it is somebody's whether or not they are there.
/// </summary>
public class Presence
{
    [Fact]
    public void Rule_10_2_leaving_a_table_that_has_not_dealt_empties_the_chair()
    {
        var table = Opened.For(3);

        table.Join();
        table.Away(1);

        Assert.Null(table.Row.Seats[1].PlayerId);
    }

    /// <summary>
    /// Rule 10.1: the hand goes on holding their cards either way, so an
    /// emptied seat would be worse than an absent player - the next arrival
    /// would be dealt into the middle of somebody else's hand.
    /// </summary>
    [Fact]
    public void Rule_10_1_but_once_it_has_dealt_the_chair_is_theirs()
    {
        var table = Opened.Filled(seats: 3).Dealt();
        var who = table.Who(1);

        table.Away(1);

        Assert.Equal(who, table.Row.Seats[1].PlayerId);
        Assert.True(table.Row.Seats[1].Away);
    }

    [Fact]
    public void Rule_10_3_and_coming_back_puts_them_at_it_again()
    {
        var table = Opened.Filled(seats: 3).Dealt();

        table.Away(1);
        table.Back(1);

        Assert.False(table.Row.Seats[1].Away);
        Assert.All(table.Hand.Away, absent => Assert.False(absent));
    }

    [Fact]
    public void Rule_10_2_and_being_at_one_table_is_being_away_from_the_others()
    {
        var table = Opened.For(3);
        var player = table.Players[0];
        var elsewhere = table.Tables
            .Open(new CreateTableRequest { Seats = 2, Jokers = 2, Mode = GameMode.Single }, Opened.Somebody())
            .Value!;

        // Seated at the first and then looking at the second, which is what
        // one connection per player makes of two pages.
        table.Tables.Join(elsewhere.TableId, player);
        table.Tables.Watching(player.PlayerId, elsewhere.TableId);

        Assert.Null(table.Tables.Find(table.Id));
    }

    [Fact]
    public void Rule_10_7_a_table_nobody_has_open_is_forgotten()
    {
        var table = Opened.Filled(seats: 2).Dealt();

        table.Away(0);

        Assert.NotNull(table.Tables.Find(table.Id));

        table.Away(1);

        Assert.Null(table.Tables.Find(table.Id));
    }

    /// <summary>
    /// Rule 10.6 is the table deciding a chair is empty of a person, and it
    /// may be right while the page is still open. A table of those is a
    /// table full of people who are about to be asked something, so closing
    /// it would be answering for them.
    /// </summary>
    [Fact]
    public void Rule_10_6_a_wait_run_out_is_absence_and_not_a_closed_page()
    {
        var table = Opened.Filled(seats: 2).Dealt();

        table.TickOut();

        Assert.Contains(true, table.Hand.Away);
        Assert.NotNull(table.Tables.Find(table.Id));
    }

    [Fact]
    public void Rule_10_3_walking_away_from_everything_leaves_every_table()
    {
        var table = Opened.For(3);

        Assert.True(table.Tables.WentAwayFromEverything(table.Who(0)));
        Assert.False(table.Tables.WentAwayFromEverything(table.Who(0)));
    }

    /// <summary>
    /// Rule 3.6 is going to act for them either way, so three players should
    /// not spend a whole turn watching a chair nobody is in.
    /// </summary>
    [Fact]
    public void Rule_3_6_a_wait_on_an_empty_chair_is_cut_short()
    {
        var table = Opened.Filled(seats: 3).Dealt();
        var waiting = table.Hand.Waiting!;

        table.Away(table.Hand.TurnSeat);

        Assert.Equal(table.Turns.FirstTurnSeconds, waiting.Seconds);
        Assert.Equal(table.Turns.AwaySeconds, table.Hand.Waiting!.Seconds);
    }

    /// <summary>
    /// A wait on nobody in particular is everybody's, so one player dropping
    /// out of it does not hurry the rest.
    /// </summary>
    [Fact]
    public void Rule_3_6_but_a_wait_on_the_whole_table_is_not()
    {
        var table = Opened.Filled(seats: 3).Cut();

        Assert.Equal(-1, table.Hand.Waiting!.Seat);

        table.Away(1);

        Assert.Equal(table.Turns.DealSeconds, table.Hand.Waiting!.Seconds);
    }

    /// <summary>
    /// Only the wait this seat was holding up. Any other and a second device
    /// dropping would hand the player in turn a whole fresh clock.
    /// </summary>
    [Fact]
    public void Rule_3_6_and_somebody_else_dropping_leaves_the_turn_alone()
    {
        var table = Opened.Filled(seats: 3).Dealt();
        var turn = table.Hand.TurnSeat;

        table.Clock.Advance(TimeSpan.FromSeconds(10));
        table.Away((turn + 1) % 3);

        Assert.Equal(table.Turns.FirstTurnSeconds - 10, table.Hand.Waiting!.SecondsLeft);
    }

}
