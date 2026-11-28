namespace Rummy51.Domain.Tests;

/// <summary>
/// Rule 9.4's half of the hand: a survival game knocks a player out and the
/// rest play on, so a hand is dealt to fewer seats than the table was
/// opened for. The seat keeps its place - everything here counts seats by
/// where they sit - and is stepped over.
/// </summary>
public class SeatsThatSitOut
{
    /// <summary>Four chairs with the third out of the game.</summary>
    private static readonly IReadOnlyList<bool> Playing = [true, true, false, true];

    private static Hand Dealt(int dealerSeat = 0)
    {
        var hand = new Hand(4, 2, dealerSeat, seed: 4242, Playing);

        hand.CutAt(20);
        hand.Deal();
        hand.Begin();

        return hand;
    }

    [Fact]
    public void Three_of_the_four_are_in_it()
    {
        var hand = Dealt();

        Assert.Equal(3, hand.Players);
        Assert.Equal(4, hand.Seats);
    }

    [Fact]
    public void Rule_2_2_the_cut_steps_over_it() => Assert.Equal(3, Dealt().CutterSeat);

    [Fact]
    public void Rule_2_4_the_seat_that_is_out_is_dealt_nothing()
    {
        var hand = Dealt();

        Assert.Empty(hand.Hands[2]);
        Assert.DoesNotContain(hand.Passes, pass => pass.Seat == 2);
    }

    [Fact]
    public void Rule_2_4_and_the_rest_are_dealt_as_they_always_were()
    {
        var hand = Dealt();

        Assert.Equal(15, hand.Hands[1].Count);
        Assert.Equal(14, hand.Hands[3].Count);
        Assert.Equal(14, hand.Hands[0].Count);
    }

    [Fact]
    public void Rule_3_1_the_opener_is_the_playing_seat_after_the_dealer() =>
        Assert.Equal(1, Dealt().TurnSeat);

    [Fact]
    public void Rule_3_2_the_turn_steps_over_it()
    {
        var hand = Dealt();
        var seats = new List<int> { hand.TurnSeat };

        foreach (var _ in Enumerable.Range(0, 5))
        {
            hand.PlayOutTheTurn(hand.TurnSeat);
            seats.Add(hand.TurnSeat);
        }

        Assert.Equal([1, 3, 0, 1, 3, 0], seats);
    }

    [Fact]
    public void Rule_6_4_a_round_is_as_long_as_the_players_and_not_the_chairs()
    {
        var hand = Dealt();

        Assert.False(hand.CanFinish);

        hand.PlayOutTheTurn(hand.TurnSeat);
        Assert.False(hand.CanFinish);

        // Three playing, so the third turn is the end of the round - a
        // fourth chair does not make it longer.
        hand.PlayOutTheTurn(hand.TurnSeat);
        Assert.True(hand.CanFinish);
    }

    [Fact]
    public void Rule_12_9_a_seat_that_is_out_cannot_force_anything() =>
        Assert.False(Dealt().CanForceARedeal(2));

    [Fact]
    public void Rule_12_3_and_does_not_hold_up_a_vote()
    {
        var hand = Dealt();
        var by = hand.TurnSeat;

        hand.OfferARedeal(by, DateTimeOffset.UnixEpoch);

        foreach (var seat in new[] { 0, 3 }.Where(one => one != by))
        {
            hand.Answer(seat, agreed: true);
        }

        // Nobody is waiting on the fourth chair: it was never asked.
        Assert.Equal(HandPhase.Answered, hand.Phase);
    }

    /// <summary>
    /// Rule 12.6 asks the seats that are in the hand for three each, and a
    /// seat that is in none of it keeps nothing. The redeal used to be
    /// counting what it had been given by every chair, so the three who
    /// answered in time took the table down with them.
    /// </summary>
    [Fact]
    public void Rule_12_6_a_seat_that_is_out_keeps_nothing_and_holds_nothing_up()
    {
        var hand = Agreed();

        foreach (var seat in new[] { 0, 1, 3 })
        {
            Assert.True(hand.Keep(seat, [.. hand.Hands[seat].Take(3).Select(held => held.Id)]));
        }

        Assert.Equal(HandPhase.Cutting, hand.Phase);
        Assert.Empty(hand.Hands[2]);
    }

    [Fact]
    public void Rule_12_6_and_is_asked_for_nothing_when_the_clock_runs_out()
    {
        var hand = Agreed();

        Assert.False(hand.Keep(2, []));
        Assert.True(hand.KeepForWhoeverHasNot());
        Assert.Empty(hand.Hands[2]);
    }

    private static Hand Agreed()
    {
        var hand = Dealt();
        var by = hand.TurnSeat;

        hand.OfferARedeal(by, DateTimeOffset.UnixEpoch);
        hand.TakeSilenceAsAgreement();
        hand.StopShowingTheResult(DateTimeOffset.UnixEpoch.AddSeconds(9));

        return hand;
    }

    [Fact]
    public void A_full_table_is_what_it_always_was()
    {
        var hand = new Hand(4, 2, 0, seed: 4242);

        hand.CutAt(20);
        hand.Deal();
        hand.Begin();

        Assert.Equal(4, hand.Players);
        Assert.Equal(3, hand.CutterSeat);
        Assert.All(hand.Hands, cards => Assert.True(cards.Count >= 14));
        Assert.Equal(1, hand.TurnSeat);
    }
}
