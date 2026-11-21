namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Rules 9.3 and 10.8: what a table does once its game is over. It asks who
/// wants another and the answers decide the rest - everybody staying deals
/// again, anybody going leaves a chair for the lobby to fill.
/// </summary>
public class AnotherGame
{
    [Fact]
    public void Rule_9_3_a_finished_game_asks_everybody_at_the_table()
    {
        var table = Opened.Arranged(seats: 3).GoOut();

        Assert.Equal(3, table.Hand.Staying.Count);
        Assert.All(table.Hand.Staying, Assert.Null);
        Assert.Equal(-1, table.Hand.Waiting!.Seat);
    }

    [Fact]
    public void Rule_9_3_and_an_answer_shows_the_moment_it_is_given()
    {
        var table = Opened.Arranged().GoOut();

        table.Tables.Stay(table.Id, table.Who(0), true);

        Assert.True(table.Hand.Staying[0]);
        Assert.Null(table.Hand.Staying[1]);
    }

    /// <summary>
    /// Nothing is left to wait for once the table has spoken. Sitting out
    /// the rest of 10.8's clock would leave everybody looking at a question
    /// they have all already answered.
    /// </summary>
    [Fact]
    public void Rule_9_3_everybody_staying_deals_again_at_once()
    {
        var table = Opened.Arranged().GoOut();

        table.Tables.Stay(table.Id, table.Who(0), true);
        table.Tables.Stay(table.Id, table.Who(1), true);

        Assert.True(table.Row.Started);
        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    /// <summary>Rule 2.1 moves the deal on exactly as it does between hands.</summary>
    [Fact]
    public void Rule_9_3_and_the_deal_moves_on_into_the_new_game()
    {
        var table = Opened.Arranged().GoOut();
        var dealt = table.Hand.DealerSeat;

        table.Tables.Stay(table.Id, table.Who(0), true);
        table.Tables.Stay(table.Id, table.Who(1), true);

        Assert.Equal((dealt + 1) % 2, table.Hand.DealerSeat);
    }

    /// <summary>Rules 9.1 and 10.4: a game of its own, starting from nothing.</summary>
    [Fact]
    public void Rule_9_1_and_the_last_game_is_not_carried_into_it()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 100).GoOut();

        table.Tables.Stay(table.Id, table.Who(0), true);
        table.Tables.Stay(table.Id, table.Who(1), true);

        Assert.All(table.Hand.Standing.Totals, total => Assert.Equal(0, total));
        Assert.All(table.Hand.Standing.Out, Assert.False);
        Assert.Empty(table.Hand.Staying);
    }

    /// <summary>
    /// Rule 10.8: the answer is the giving up, and waiting for the rest of
    /// the table to speak would leave everybody looking at somebody sitting
    /// in a chair they have already left.
    /// </summary>
    [Fact]
    public void Rule_10_8_saying_no_empties_the_chair_before_the_rest_answer()
    {
        var table = Opened.Arranged(seats: 3).GoOut();

        table.Tables.Stay(table.Id, table.Who(0), false);

        Assert.Null(table.Row.Seats[0].PlayerId);
        Assert.NotNull(table.Row.Seats[1].PlayerId);
    }

    /// <summary>
    /// Rule 9.3 is put to players and not to chairs. Rule 9.6 empties one
    /// in the middle of a game, and an empty one can never answer: a table
    /// that waited for it would sit out the whole of 10.8's clock with
    /// everybody who is there already agreed.
    /// </summary>
    [Fact]
    public void Rule_9_3_a_chair_nobody_is_in_is_never_waited_for()
    {
        var table = Opened.Arranged(seats: 3, mode: GameMode.Survival, limit: 100).GoOut();
        var won = table.Hand.WinnerSeat;

        foreach (var seat in Enumerable.Range(0, 3).Where(one => one != won))
        {
            table.Tables.GiveUpTheChair(table.Id, table.Who(seat));
        }

        table.Tables.Stay(table.Id, table.Who(won), true);

        Assert.False(table.Row.Started);
        Assert.Equal(1, table.Row.Seats.Count(seat => seat.PlayerId is not null));
    }

    /// <summary>
    /// Somebody seated but not looking is waited for all the same. Not
    /// answering costs them the chair, and 10.8 is the time it gives them.
    /// </summary>
    [Fact]
    public void Rule_10_8_but_a_seated_player_who_says_nothing_is()
    {
        var table = Opened.Arranged(seats: 3).GoOut();

        table.Away(1);
        table.Tables.Stay(table.Id, table.Who(0), true);
        table.Tables.Stay(table.Id, table.Who(2), true);

        Assert.True(table.Row.Started);
        Assert.NotNull(table.Row.Seats[1].PlayerId);
    }

    /// <summary>Rule 10.8: silence is going, which is the other way round from 12.4.</summary>
    [Fact]
    public void Rule_10_8_and_silence_at_the_whole_table_empties_it()
    {
        var table = Opened.Arranged().GoOut();
        var swept = table.Tick(table.Turns.StaySeconds);

        Assert.True(swept.ListChanged);
        Assert.Null(table.Tables.Find(table.Id));
    }

    /// <summary>
    /// Rule 9.3: a table that had a chair to fill deals as a new one, the
    /// question rule 2.1 answers no longer being the same question.
    /// </summary>
    [Fact]
    public void Rule_9_3_a_chair_given_up_leaves_the_table_waiting_again()
    {
        var table = Opened.Arranged(seats: 3).GoOut();

        table.Tables.Stay(table.Id, table.Who(0), false);
        table.Tables.Stay(table.Id, table.Who(1), true);
        table.Tables.Stay(table.Id, table.Who(2), true);

        Assert.False(table.Row.Started);
        Assert.Null(table.Tables.Hand(table.Id));

        table.Join();

        Assert.True(table.Row.Started);
        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    /// <summary>
    /// The people who need to hear it are the ones not sitting here, and
    /// nothing else would ever say so.
    /// </summary>
    [Fact]
    public void Rule_9_3_and_the_lobby_is_told_the_row_has_changed()
    {
        var table = Opened.Arranged(seats: 3).GoOut();

        table.Tables.Stay(table.Id, table.Who(0), false);
        table.Tables.Stay(table.Id, table.Who(1), true);
        table.Tables.Stay(table.Id, table.Who(2), true);

        Assert.True(table.Tick(0).ListChanged);
        Assert.False(table.Tick(0).ListChanged);
    }

    /// <summary>Rule 10.8 gives them the time, and it is 10.8's own.</summary>
    [Fact]
    public void Rule_10_8_and_the_question_is_asked_for_its_own_length()
    {
        var turns = new TurnSettings { StaySeconds = 41, TurnSeconds = 17 };
        var table = Opened.Arranged(turns: turns).GoOut();

        Assert.Equal(41, table.Hand.Waiting!.Seconds);
    }
}
