using Rummy51.Application.Games;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// A table is memory and a game is not. Nothing here decides anything: it
/// watches for the three moments worth keeping and queues a description of
/// each for somebody else to write down, because a table must not wait on
/// a database.
/// </summary>
public class WhatOutlivesTheTable
{
    [Fact]
    public void Rule_9_1_a_game_is_written_down_the_moment_it_begins()
    {
        var table = Opened.Filled(seats: 3, mode: GameMode.Survival, limit: 200, jokers: 4);
        var began = Assert.IsType<GameBegan>(Assert.Single(table.Tables.TakeRecords()));

        Assert.Equal(table.Id, began.TableId);
        Assert.Equal(GameMode.Survival, began.Mode);
        Assert.Equal(200, began.Target);
        Assert.Equal(3, began.Seats);
        Assert.Equal(4, began.Jokers);
    }

    /// <summary>
    /// Rule 10.4: it begins with the players it will end with. Rule 9.6
    /// empties the chair of whoever is out, so by the time the game ends
    /// the seats no longer hold everybody who played it.
    /// </summary>
    [Fact]
    public void Rule_10_4_and_with_who_was_sitting_where()
    {
        var table = Opened.Filled(seats: 3);
        var began = (GameBegan)table.Tables.TakeRecords()[0];

        Assert.Equal(
            Enumerable.Range(0, 3).Select(table.Who),
            began.Players.OrderBy(player => player.Seat).Select(player => player.PlayerId));
    }

    /// <summary>Taken, not read: what is taken once is not handed over twice.</summary>
    [Fact]
    public void Nothing_is_written_down_more_than_once()
    {
        var table = Opened.Filled(seats: 3);

        Assert.Single(table.Tables.TakeRecords());
        Assert.Empty(table.Tables.TakeRecords());
    }

    [Fact]
    public void Rule_6_1_a_hand_that_ended_is_written_down_with_it()
    {
        var table = Opened.Arranged(seats: 3, mode: GameMode.Survival, limit: 500).GoOut();
        var ended = table.Tables.TakeRecords().OfType<HandEnded>().Single();

        Assert.Equal(1, ended.Number);
        Assert.Equal(table.Hand.DealerSeat, ended.DealerSeat);
        Assert.Equal(3, ended.Results.Count);
    }

    /// <summary>
    /// Rule 11.5 kept, so an odd hand can be dealt again exactly as it was.
    /// The deck is written out as well as the seed, because a restock is
    /// shuffled from whatever the table happened to throw and no seed
    /// reaches one.
    /// </summary>
    [Fact]
    public void Rule_11_5_the_deal_is_kept_so_it_can_be_laid_out_again()
    {
        var table = Opened.Arranged(seats: 3, mode: GameMode.Survival, limit: 500).GoOut();
        var ended = table.Tables.TakeRecords().OfType<HandEnded>().Single();

        Assert.NotEmpty(ended.DeckOrder);
        Assert.Equal(table.Hand.DeckSize, ended.DeckOrder.Split(',').Length);
    }

    /// <summary>
    /// Rule 9.5: a game already played keeps the standing it was played to,
    /// which could be added up again out of the hands and is written down
    /// all the same.
    /// </summary>
    [Fact]
    public void Rule_9_5_a_game_that_ended_keeps_where_it_placed_everybody()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 100).GoOut();
        var ended = table.Tables.TakeRecords().OfType<GameEnded>().Single();
        var won = table.Hand.WinnerSeat;

        Assert.Equal(table.Who(won), ended.WinnerId);
        Assert.Equal(2, ended.Standing.Count);

        var winner = ended.Standing.Single(one => one.PlayerId == table.Who(won));

        Assert.Equal(1, winner.Place);
        Assert.Equal(1, winner.HandsPlayed);
        Assert.Null(winner.OutAfterHand);
    }

    /// <summary>Rule 9.4, as the number of the hand that put them out.</summary>
    [Fact]
    public void Rule_9_4_and_which_hand_put_each_of_them_out()
    {
        var table = Opened.Arranged(mode: GameMode.Survival, limit: 100).GoOut();
        var ended = table.Tables.TakeRecords().OfType<GameEnded>().Single();
        var lost = ended.Standing.Single(one => one.PlayerId != table.Who(table.Hand.WinnerSeat));

        Assert.Equal(1, lost.OutAfterHand);
        Assert.Equal(2, lost.Place);
    }
}
