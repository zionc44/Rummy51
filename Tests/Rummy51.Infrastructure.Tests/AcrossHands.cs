namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Chapter 9: the game a hand belongs to. A hand knows nothing of it - it
/// is dealt, played and scored the same way whichever game it is one of -
/// so everything that runs across hands is counted here instead.
/// </summary>
public class AcrossHands
{
    [Fact]
    public void Rule_6_1_a_hand_ends_when_somebody_is_out_of_cards()
    {
        var table = Opened.Arranged().GoOut();

        Assert.Equal(HandPhase.Finished, table.Hand.Phase);
        Assert.NotEqual(-1, table.Hand.WinnerSeat);
    }

    /// <summary>
    /// What a hand is worth is chapter 8's and is held to in the domain's
    /// own suite. What is asked here is only that it lands in the total,
    /// which is the half of it that lives across hands.
    /// </summary>
    [Fact]
    public void Rule_8_1_what_the_hand_came_to_goes_into_the_running_total()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();
        var won = table.Hand.WinnerSeat;

        Assert.Equal(table.Hand.Scores[won], table.Hand.Standing.Totals[won]);
        Assert.Equal(table.Hand.Scores[1 - won], table.Hand.Standing.Totals[1 - won]);
        Assert.Equal(Enumerable.Repeat(1, 2), table.Hand.Standing.Hands);
    }

    /// <summary>
    /// Rule 9.4 would deal at once. The pause is the table being read
    /// rather than the game being slow, so it is over as soon as everybody
    /// has read it.
    /// </summary>
    [Fact]
    public void Rule_9_4_the_next_hand_waits_only_to_be_read()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();

        Assert.Equal(HandPhase.Finished, table.Hand.Phase);

        table.Tables.ReadTheHand(table.Id, table.Who(0));

        Assert.Equal(HandPhase.Finished, table.Hand.Phase);

        table.Tables.ReadTheHand(table.Id, table.Who(1));

        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    [Fact]
    public void Rule_9_4_and_the_clock_reads_it_for_whoever_did_not()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();

        table.Tick(table.Turns.BetweenHandsSeconds);

        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    /// <summary>
    /// Whoever is not watching cannot press anything, and the clock is what
    /// covers them - so one player reading it is the whole table where the
    /// other has gone.
    /// </summary>
    [Fact]
    public void Rule_9_4_a_seat_nobody_is_at_is_not_waited_for()
    {
        var table = Opened.Arranged(seats: 3, mode: GameMode.Survival, limit: 500).GoOut();

        table.Away(2);
        table.Tables.ReadTheHand(table.Id, table.Who(0));
        table.Tables.ReadTheHand(table.Id, table.Who(1));

        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    [Fact]
    public void Rule_2_1_the_deal_moves_on_by_one()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();
        var dealt = table.Hand.DealerSeat;

        table.Tick(table.Turns.BetweenHandsSeconds);

        Assert.Equal((dealt + 1) % 2, table.Hand.DealerSeat);
    }

    /// <summary>Reaching the number is enough, and it is the end of this hand.</summary>
    [Fact]
    public void Rule_9_4_reaching_the_number_puts_a_player_out_of_the_game()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 100).GoOut();
        var lost = 1 - table.Hand.WinnerSeat;

        Assert.True(table.Hand.Standing.Totals[lost] >= 100);
        Assert.True(table.Hand.Standing.Out[lost]);
        Assert.False(table.Hand.Standing.Out[table.Hand.WinnerSeat]);
    }

    /// <summary>Rule 9.5: the last one left in it, and only their score is a minus.</summary>
    [Fact]
    public void Rule_9_5_and_the_one_still_in_it_has_won_the_game()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 100).GoOut();
        var won = table.Hand.WinnerSeat;

        Assert.Equal([won], table.Hand.Standing.Won);
        Assert.Equal(1, table.Hand.Standing.Places[won]);
        Assert.Equal(2, table.Hand.Standing.Places[1 - won]);
    }

    /// <summary>
    /// Rule 9.2: a single hand is the whole of its game, so there is no
    /// next one to deal and the table goes straight to asking.
    /// </summary>
    [Fact]
    public void Rule_9_2_a_single_hand_is_the_whole_of_its_game()
    {
        var table = Opened.Arranged().GoOut();

        Assert.NotEmpty(table.Hand.Staying);
    }

    [Fact]
    public void Rule_9_2_and_a_survival_game_is_not_over_while_two_are_in_it()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();

        Assert.Empty(table.Hand.Staying);
    }

    /// <summary>
    /// The pause is the table being read rather than the game being slow,
    /// so it is its own length and not a turn's.
    /// </summary>
    [Fact]
    public void Rule_9_4_and_the_pause_between_hands_is_its_own_length()
    {
        var turns = new TurnSettings { BetweenHandsSeconds = 42, TurnSeconds = 17 };
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500, turns: turns).GoOut();

        Assert.Equal(42, table.Hand.Waiting!.Seconds);
    }
    /// <summary>
    /// Rule 9.2: a game is a sequence of hands, and what each of them came
    /// to outlives it. The total is the column added up, so the two can be
    /// read against each other.
    /// </summary>
    [Fact]
    public void Rule_9_2_every_hand_of_the_game_keeps_what_it_came_to()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500);
        var first = table.GoOut().Hand.Scores.ToArray();
        var pad = TheHandAfter(table).Hand.Standing.EachHand;

        Assert.Equal(2, pad.Count);
        Assert.Equal(first.Select(score => (int?)score), pad[0]);
        Assert.Equal(table.Hand.Scores.Select(score => (int?)score), pad[1]);
    }

    [Fact]
    public void Rule_9_2_and_what_each_of_them_stands_at_is_their_column_added_up()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500).GoOut();
        var standing = TheHandAfter(table).Hand.Standing;

        Assert.Equal(
            standing.Totals,
            Enumerable.Range(0, 2).Select(seat => standing.EachHand.Sum(hand => hand[seat] ?? 0)));
    }

    [Fact]
    public void Rule_9_2_and_a_game_with_no_hand_behind_it_has_nothing_in_it()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500);

        Assert.Empty(table.Hand.Standing.EachHand);
    }

    /// <summary>
    /// Rule 9.4 as the screen has to read it: a seat watching a hand it is
    /// out of is asked none of the questions the hand puts, and the only
    /// way the screen can tell is for the view to say so.
    /// </summary>
    [Fact]
    public void Rule_9_4_the_view_names_the_seats_the_hand_was_dealt_to()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500);

        Assert.Equal([true, true], table.Hand.Playing);
    }

    /// <summary>The wait between hands read out, dealt again, and played.</summary>
    private static Opened TheHandAfter(Opened table)
    {
        table.Tick(table.Turns.BetweenHandsSeconds);

        return table.Dealt().GoOut();
    }
}