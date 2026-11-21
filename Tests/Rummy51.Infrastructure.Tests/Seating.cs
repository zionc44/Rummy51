using Rummy51.Application.Authentication;
using Rummy51.Application.Common;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Opening a table and joining one, which are the same move: a player holds
/// at most one chair, so both ask the same question before anything else.
/// </summary>
public class Seating
{
    [Fact]
    public void Rule_1_1_a_table_is_for_two_three_or_four()
    {
        Assert.Equal("TableSeatsInvalid", Refusing(seats: 5).Errors[0].Code);
        Assert.Equal("TableSeatsInvalid", Refusing(seats: 1).Errors[0].Code);
    }

    [Fact]
    public void Rule_1_1_and_carries_none_two_or_four_jokers()
    {
        Assert.Equal("TableJokersInvalid", Refusing(jokers: 1).Errors[0].Code);
        Assert.Equal("TableJokersInvalid", Refusing(jokers: 6).Errors[0].Code);
    }

    [Fact]
    public void Rule_9_2_a_single_hand_runs_to_nothing()
    {
        Assert.Equal("TableLimitInvalid", Refusing(limit: 100).Errors[0].Code);
    }

    [Fact]
    public void Rule_9_2_survival_runs_to_one_of_three_numbers()
    {
        Assert.True(Opening(mode: GameMode.Survival, limit: 200).IsSuccess);
        Assert.Equal(
            "TableLimitInvalid",
            Refusing(mode: GameMode.Survival, limit: 150).Errors[0].Code);
    }

    /// <summary>Rule 2.1 would give somebody an extra deal if it did not divide.</summary>
    [Fact]
    public void Rule_9_2_and_a_series_runs_to_hands_the_seats_divide()
    {
        Assert.True(Opening(seats: 3, mode: GameMode.Series, limit: 6).IsSuccess);
        Assert.Equal(
            "TableLimitInvalid",
            Refusing(seats: 3, mode: GameMode.Series, limit: 4).Errors[0].Code);
    }

    [Fact]
    public void The_one_who_opened_it_is_sitting_in_the_first_chair()
    {
        var table = Opened.For(3);

        Assert.Equal(table.Who(0), table.Row.Seats[0].PlayerId);
        Assert.All(table.Row.Seats.Skip(1), seat => Assert.Null(seat.PlayerId));
    }

    [Fact]
    public void A_newcomer_takes_the_first_chair_that_is_free()
    {
        var table = Opened.For(3);
        var second = table.Join();

        Assert.Equal(second.PlayerId, table.Row.Seats[1].PlayerId);
    }

    [Fact]
    public void A_player_waiting_at_one_table_may_not_open_another()
    {
        var table = Opened.For(3);
        var again = table.Tables.Open(Asking(), table.Players[0]);

        Assert.Equal(FailureKind.Conflict, again.Failure);
        Assert.Equal("AlreadyAtATable", again.Errors[0].Code);
    }

    [Fact]
    public void Nor_join_one()
    {
        var table = Opened.For(3);
        var other = table.Tables.Open(Asking(), Opened.Somebody()).Value!;
        var joining = table.Tables.Join(other.TableId, table.Players[0]);

        Assert.Equal("AlreadyAtATable", joining.Errors[0].Code);
    }

    /// <summary>Which is what reopening the page on a table looks like.</summary>
    [Fact]
    public void Arriving_at_the_table_already_sat_at_is_not_a_failure()
    {
        var table = Opened.For(3);
        var again = table.Tables.Join(table.Id, table.Players[0]);

        Assert.True(again.IsSuccess);
        Assert.Equal(table.Who(0), again.Value!.Seats[0].PlayerId);
    }

    [Fact]
    public void A_table_nobody_opened_cannot_be_joined()
    {
        var table = Opened.For(3);
        var joining = table.Tables.Join(Guid.NewGuid(), Opened.Somebody());

        Assert.Equal(FailureKind.Validation, joining.Failure);
        Assert.Equal("TableNotFound", joining.Errors[0].Code);
    }

    [Fact]
    public void Rule_10_1_and_one_whose_hand_has_begun_is_closed_to_newcomers()
    {
        var table = Opened.Filled(seats: 2);
        var joining = table.Tables.Join(table.Id, Opened.Somebody());

        Assert.Equal(FailureKind.Conflict, joining.Failure);
        Assert.Equal("TableStarted", joining.Errors[0].Code);
    }

    /// <summary>
    /// Rule 2.2: there is no button. The last chair being taken is what
    /// deals, and what the table is then waiting for is the cut.
    /// </summary>
    [Fact]
    public void Rule_2_2_filling_the_last_chair_begins_the_hand()
    {
        var table = Opened.For(2);

        Assert.Null(table.Tables.Hand(table.Id));

        table.Join();

        Assert.True(table.Row.Started);
        Assert.Equal(HandPhase.Cutting, table.Hand.Phase);
    }

    /// <summary>
    /// Version 7 ids are time ordered, so sorting by id lists them roughly
    /// in the order they were opened - roughly because the stamp is only
    /// as fine as a millisecond, and two tables opened inside one of those
    /// sort by the random tail. What is asserted here is the sort itself,
    /// which is what keeps the lobby off whatever order a dictionary feels
    /// like handing back.
    /// </summary>
    [Fact]
    public void The_lobby_lists_tables_sorted_rather_than_as_they_are_held()
    {
        var table = Opened.For(3);

        table.Tables.Open(Asking(), Opened.Somebody());
        table.Tables.Open(Asking(), Opened.Somebody());

        var listed = table.Tables.All().Select(one => one.TableId).ToList();

        Assert.Equal(3, listed.Count);
        Assert.Equal(listed.Order(), listed);
    }

    private static Result<LobbyTable> Refusing(
        int seats = 3,
        int jokers = 2,
        GameMode mode = GameMode.Single,
        int? limit = null)
    {
        var refused = Opening(seats, jokers, mode, limit);

        Assert.False(refused.IsSuccess);

        return refused;
    }

    private static Result<LobbyTable> Opening(
        int seats = 3,
        int jokers = 2,
        GameMode mode = GameMode.Single,
        int? limit = null) =>
        Opened.None().Open(Asking(seats, jokers, mode, limit), Opened.Somebody());

    private static CreateTableRequest Asking(
        int seats = 3,
        int jokers = 2,
        GameMode mode = GameMode.Single,
        int? limit = null) =>
        new() { Seats = seats, Jokers = jokers, Mode = mode, Limit = limit };

}
