namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// The third kind of game: a fixed number of hands, some of them counting
/// for more than the rest. What each hand counts for is the domain's; what
/// is asked here is that the table deals it, shows it and ends on time.
/// </summary>
public class ASeries
{
    [Fact]
    public void Rule_8_4_every_hand_of_a_series_is_shown_from_the_first_deal()
    {
        var table = Opened.Arranged(mode: GameMode.Series, limit: 4);

        Assert.Equal([1, 2, 1, 4], table.Hand.Standing.Weights.Select(weight => weight.Fixed));
    }

    [Fact]
    public void Rule_8_4_and_no_other_game_has_any()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 500);

        Assert.Empty(table.Hand.Standing.Weights);
    }

    [Fact]
    public void Rule_8_4_the_hand_dealt_is_told_what_it_counts_for()
    {
        var table = Opened.Arranged(mode: GameMode.Series, limit: 4).GoOut();

        Assert.Equal(1, table.Hand.Doubling.ForTheSeries);

        TheNext(table).GoOut();

        Assert.Equal(2, table.Hand.Doubling.ForTheSeries);
    }

    /// <summary>
    /// The second of four already counts double, so the redeal agreed in it
    /// is on the third the moment the table agrees - and the second, dealt
    /// again and won, is doubled for the series and not for its redeal.
    /// </summary>
    [Fact]
    public void Rule_8_5_a_redeal_in_a_doubled_hand_is_shown_on_the_next_single_one()
    {
        var table = TheNext(Opened.Arranged(mode: GameMode.Series, limit: 4).GoOut());

        table.Tables.OfferARedeal(table.Id, table.Who(table.Hand.TurnSeat));
        table.Tick(table.Turns.OfferSeconds);
        table.Tick(table.Turns.ResultSeconds);

        Assert.Equal(HandPhase.Keeping, table.Hand.Phase);

        Assert.Equal([false, false, true, false], table.Hand.Standing.Weights.Select(weight => weight.Redealt));

        table.TickOut();
        table.Dealt().GoOut();

        Assert.Equal(2, table.Hand.Doubling.ForTheSeries);
        Assert.Equal(1, table.Hand.Doubling.ForTheRedeal);
    }

    [Fact]
    public void Rule_9_7_a_series_is_not_over_before_its_last_hand()
    {
        var table = Opened.Arranged(mode: GameMode.Series, limit: 4).GoOut();

        for (var hand = 2; hand < 4; hand++)
        {
            Assert.Empty(table.Hand.Staying);
            TheNext(table).GoOut();
        }

        Assert.Empty(table.Hand.Staying);
    }

    [Fact]
    public void Rule_9_7_and_is_over_at_the_end_of_it()
    {
        var table = AllFour();

        Assert.NotEmpty(table.Hand.Staying);
        Assert.Equal([4, 4], table.Hand.Standing.Hands);
    }

    [Fact]
    public void Rule_9_8_a_series_is_won_by_the_lowest_total()
    {
        var standing = AllFour().Hand.Standing;
        var lowest = standing.Totals.ToList().IndexOf(standing.Totals.Min());

        Assert.Equal([lowest], standing.Won);
        Assert.Equal(1, standing.Places[lowest]);
    }

    [Fact]
    public void Rule_9_8_and_nobody_has_won_it_before_it_ends()
    {
        var table = Opened.Arranged(mode: GameMode.Series, limit: 4).GoOut();

        Assert.Empty(table.Hand.Standing.Won);
    }

    private static Opened AllFour()
    {
        var table = Opened.Arranged(mode: GameMode.Series, limit: 4).GoOut();

        for (var hand = 2; hand <= 4; hand++)
        {
            TheNext(table).GoOut();
        }

        return table;
    }

    /// <summary>The wait between hands read out and the next one dealt.</summary>
    private static Opened TheNext(Opened table)
    {
        table.Tick(table.Turns.BetweenHandsSeconds);

        return table.Dealt();
    }
}
