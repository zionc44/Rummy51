using Rummy51.Application.Common;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Who is asking, which is this project's half of a move: the hand owns the
/// rules and this owns whether the person asking is anybody. A refusal is
/// one code per move rather than a reason, because a client only ever
/// offers a move it believes is legal - one that comes back refused is a
/// race or a bug, and neither has anything to tell the player.
///
/// Two of them do have something to tell: both are rules of going down that
/// the screen cannot see coming, and the wrong one named is worse than none.
/// </summary>
public class Refusals
{
    [Fact]
    public void A_move_at_a_table_that_is_not_there_says_so()
    {
        var table = Opened.Filled(seats: 2).Dealt();
        var refused = table.Tables.Draw(Guid.NewGuid(), table.Who(0), DrawnFrom.Stock);

        Assert.Equal(FailureKind.Validation, refused.Failure);
        Assert.Equal("TableNotFound", refused.Errors[0].Code);
    }

    [Fact]
    public void And_one_by_somebody_who_is_not_sitting_at_it()
    {
        var table = Opened.Filled(seats: 2).Dealt();
        var refused = table.Tables.Draw(table.Id, Guid.NewGuid(), DrawnFrom.Stock);

        Assert.Equal("NotAtThisTable", refused.Errors[0].Code);
    }

    [Fact]
    public void Rule_3_2_and_one_out_of_turn_is_a_race_with_nothing_to_say()
    {
        var table = Opened.Filled(seats: 2).Dealt();
        var waiting = 1 - table.Hand.TurnSeat;
        var refused = table.Tables.Draw(table.Id, table.Who(waiting), DrawnFrom.Stock);

        Assert.Equal(FailureKind.Conflict, refused.Failure);
        Assert.Equal("CannotDraw", refused.Errors[0].Code);
    }

    [Fact]
    public void Rule_2_2_a_cut_is_only_the_cutter_s_to_make()
    {
        var table = Opened.Filled(seats: 3);
        var somebody = (table.Hand.CutterSeat + 1) % 3;
        var refused = table.Tables.CutDeck(table.Id, table.Who(somebody), 0);

        Assert.Equal("NotYoursToCut", refused.Errors[0].Code);
    }

    [Fact]
    public void Rule_2_2_and_a_deck_is_only_cut_once()
    {
        var table = Opened.Filled(seats: 3).Cut();
        var refused = table.Tables.CutDeck(table.Id, table.Who(table.Hand.CutterSeat), 0);

        Assert.Equal(FailureKind.Conflict, refused.Failure);
        Assert.Equal("AlreadyCut", refused.Errors[0].Code);
    }

    [Fact]
    public void Rule_2_2_and_a_table_that_has_not_filled_has_nothing_to_cut()
    {
        var table = Opened.For(3);
        var refused = table.Tables.CutDeck(table.Id, table.Who(0), 0);

        Assert.Equal("HandNotStarted", refused.Errors[0].Code);
    }

    /// <summary>
    /// Rule 5.1: what a turn has laid has to come to fifty-one, and that is
    /// asked when the throw ends the turn, since until the throw lands
    /// anything can still be taken back.
    /// </summary>
    [Fact]
    public void Rule_5_1_a_throw_on_a_showing_that_is_short_says_which_rule_caught_them()
    {
        var table = Opened.Arranged();
        var seat = table.Hand.TurnSeat;
        var player = table.Who(seat);
        var cards = table.Tables.CardsOf(table.Id, player).Select(card => card.Id).ToList();

        // The three kings and nothing else: thirty, where fifty-one is asked.
        table.Tables.LayDown(table.Id, player, [.. cards[8..11]]);

        var refused = table.Tables.Discard(table.Id, player, cards[^1]);

        Assert.Equal("GoingDownIsShort", refused.Errors[0].Code);
    }

    /// <summary>
    /// Rules 6.2 and 6.5 keep cards back in the hand, and nothing the client
    /// knows says how many - which is why this one is the player's to do
    /// something about rather than a race.
    /// </summary>
    [Fact]
    public void Rule_6_5_and_laying_the_last_of_a_first_round_hand_says_the_same()
    {
        var table = Opened.Arranged();
        var seat = table.Hand.TurnSeat;
        var player = table.Who(seat);
        var cards = table.Tables.CardsOf(table.Id, player).Select(card => card.Id).ToList();

        table.Tables.LayDown(table.Id, player, [.. cards[0..4]]);
        table.Tables.LayDown(table.Id, player, [.. cards[4..8]]);
        table.Tables.LayDown(table.Id, player, [.. cards[8..11]]);

        // Which would leave one card where the first round asks for two.
        var refused = table.Tables.LayDown(table.Id, player, [.. cards[11..14]]);

        Assert.Equal("KeepTwoInTheFirstRound", refused.Errors[0].Code);
    }

    /// <summary>
    /// Rule 5.10: whatever is taken off the table is back on it before the
    /// turn ends, and the card off the discard pile is that rule and not
    /// another one.
    /// </summary>
    [Fact]
    public void Rule_5_10_a_throw_still_holding_what_was_taken_says_so()
    {
        var table = Opened.Filled(seats: 2).Dealt();

        table.PlayOut();

        var player = table.Who(table.Hand.TurnSeat);

        Assert.True(table.Tables.Draw(table.Id, player, DrawnFrom.Discards).IsSuccess);

        var held = table.Tables.CardsOf(table.Id, player);
        var other = held.First(card => !card.Owed).Id;
        var refused = table.Tables.Discard(table.Id, player, other);

        Assert.Equal("TakenCardStillHeld", refused.Errors[0].Code);
    }
}
