using static Rummy51.Domain.Tests.Cards;

namespace Rummy51.Domain.Tests;

/// <summary>
/// Going out, and what the hand then came to. These
/// are the rules this project has got wrong most often, so they are held to
/// a hand actually played out rather than to a number worked out beside it.
/// </summary>
public class TheScore
{
    [Fact]
    public void Rule_6_1_a_hand_ends_when_somebody_is_out_of_cards()
    {
        var (hand, seat) = InOneStrike();

        Assert.Equal(HandPhase.Finished, hand.Phase);
        Assert.Equal(seat, hand.WinnerSeat);
        Assert.Empty(hand.Hands[seat]);
    }

    [Fact]
    public void Rule_8_1_the_winner_takes_minus_thirty_and_the_rest_pay()
    {
        var (hand, seat) = AcrossTwoTurns();

        Assert.Equal(-30, hand.Scores[seat]);
        Assert.True(hand.Scores[1 - seat] > 0);
    }

    [Fact]
    public void Rule_8_1_a_hand_nobody_showed_is_a_hundred_whatever_is_in_it()
    {
        var (hand, seat) = AcrossTwoTurns();
        var other = 1 - seat;

        Assert.False(hand.WentDown[other]);
        Assert.Equal(Worth.NeverWentDown, hand.Scores[other]);
    }

    [Fact]
    public void Rule_6_6_going_out_in_one_strike_is_rummy_and_doubles_it()
    {
        var (hand, seat) = InOneStrike();

        Assert.True(hand.WentRummy[seat]);
        Assert.Equal(2, hand.WinMultiplier);
        Assert.Equal(2, hand.ResultMultiplier);
        Assert.Equal(-60, hand.Scores[seat]);
    }

    [Fact]
    public void Rule_6_6_and_laying_across_two_turns_is_not()
    {
        var (hand, seat) = AcrossTwoTurns();

        Assert.False(hand.WentRummy[seat]);
        Assert.Equal(1, hand.ResultMultiplier);
    }

    [Fact]
    public void Rule_6_6_a_card_put_on_a_run_laid_in_the_same_strike_is_still_rummy()
    {
        var (hand, seat) = InOneStrike(attaching: true);

        Assert.True(hand.WentRummy[seat]);
        Assert.Equal(-60, hand.Scores[seat]);
    }

    [Fact]
    public void Rule_8_2_doubling_is_blanket_and_falls_on_everybody()
    {
        var (hand, seat) = InOneStrike();

        Assert.Equal(-60, hand.Scores[seat]);
        Assert.Equal(Worth.NeverWentDown * 2, hand.Scores[1 - seat]);
    }

    [Fact]
    public void Rule_8_6_rummy_in_a_hand_counting_four_times_over_counts_eight()
    {
        var (hand, seat) = InOneStrike(weight: new HandWeight(4, false));

        Assert.Equal(8, hand.ResultMultiplier);
        Assert.Equal(-240, hand.Scores[seat]);
        Assert.Equal(Worth.NeverWentDown * 8, hand.Scores[1 - seat]);
    }

    /// <summary>A redeal an earlier hand sent on doubles like one agreed in this hand.</summary>
    [Fact]
    public void Rule_8_5_a_redeal_sent_on_doubles_the_hand_it_falls_on()
    {
        var (hand, _) = InOneStrike(weight: new HandWeight(1, true));

        Assert.Equal(1, hand.FixedMultiplier);
        Assert.Equal(2, hand.RedealMultiplier);
        Assert.Equal(4, hand.ResultMultiplier);
    }

    [Fact]
    public void Rule_8_4_a_hand_counting_double_doubles_everybody()
    {
        var (hand, seat) = AcrossTwoTurns(new HandWeight(2, false));

        Assert.Equal(2, hand.FixedMultiplier);
        Assert.Equal(1, hand.RedealMultiplier);
        Assert.Equal(-60, hand.Scores[seat]);
    }

    [Fact]
    public void Rule_8_2_an_ordinary_win_doubles_nothing()
    {
        var (hand, _) = AcrossTwoTurns();

        Assert.Equal(1, hand.RedealMultiplier);
        Assert.Equal(1, hand.WinMultiplier);
    }

    [Fact]
    public void Rule_8_1_nothing_is_scored_until_the_hand_is_over()
    {
        var hand = Dealt.Shuffled();

        Assert.Empty(hand.Scores);
        Assert.Empty(hand.Places);
        Assert.Equal(1, hand.ResultMultiplier);
    }

    [Fact]
    public void Rule_8_3_the_winner_is_placed_first()
    {
        var (hand, seat) = InOneStrike();

        Assert.Equal(1, hand.Places[seat]);
        Assert.Equal(2, hand.Places[1 - seat]);
    }

    [Fact]
    public void Rule_7_1_the_finishing_card_is_there_to_be_taken()
    {
        var hand = Dealt.Shuffled(seats: 3);

        // Rule 6.4 closes it for a round, and 7.1 asks the same of a player
        // as going out does.
        Assert.False(hand.CanTakeTheFinishingCard(hand.TurnSeat));

        hand.PlayOutTheTurn(hand.TurnSeat);
        hand.PlayOutTheTurn(hand.TurnSeat);

        Assert.NotNull(hand.Deck.FinishingCard);
        Assert.True(hand.CanFinish);
    }

    [Fact]
    public void Rule_7_1_and_not_by_somebody_who_is_already_down()
    {
        var (hand, seat) = AcrossTwoTurns();

        Assert.True(hand.WentDown[seat]);
        Assert.False(hand.CanTakeTheFinishingCard(seat));
    }

    // ------------------------------------------------------------------------

    /// <summary>
    /// A hand taken in one turn: the odd card goes on an ordinary first
    /// turn, because rule 6.5 keeps two back while going out is closed, and
    /// everything else goes down on the second.
    /// </summary>
    private static (Hand Hand, int Seat) InOneStrike(bool attaching = false, HandWeight? weight = null)
    {
        var hand = Dealt.Holding(Dealt.AWinningHand, weight: weight);
        var seat = hand.TurnSeat;
        var cards = TheTurn.Held(hand, seat);

        hand.Discard(seat, cards[^1]);
        RoundTo(hand, seat);

        var drawn = Drawn(hand, seat, cards);

        if (attaching)
        {
            // Three of three and one of four, and the fifteenth card put on
            // the run of spades afterwards.
            hand.LayDown(seat, [.. cards[4..8]]);
            hand.LayDown(seat, [.. cards[8..11]]);
            hand.LayDown(seat, [.. cards[11..14]]);
            hand.LayDown(seat, [.. cards[0..3]]);

            hand.AddTo(seat, hand.Board.Melds.Count - 1, cards[3], MeldEnd.Back);
        }
        else
        {
            hand.LayDown(seat, [.. cards[0..4]]);
            hand.LayDown(seat, [.. cards[4..8]]);
            hand.LayDown(seat, [.. cards[8..11]]);
            hand.LayDown(seat, [.. cards[11..14]]);
        }

        hand.Discard(seat, drawn);

        return (hand, seat);
    }

    /// <summary>The same hand laid over two turns, which rule 6.6 does not double.</summary>
    private static (Hand Hand, int Seat) AcrossTwoTurns(HandWeight? weight = null)
    {
        var hand = Dealt.Holding(Dealt.AWinningHand, weight: weight);
        var seat = hand.TurnSeat;
        var cards = TheTurn.Held(hand, seat);

        hand.Discard(seat, cards[^1]);
        RoundTo(hand, seat);

        var drawn = Drawn(hand, seat, cards);

        hand.LayDown(seat, [.. cards[0..4]]);
        hand.LayDown(seat, [.. cards[4..8]]);
        hand.Discard(seat, drawn);

        RoundTo(hand, seat);

        var next = Drawn(hand, seat, cards);

        hand.LayDown(seat, [.. cards[8..11]]);
        hand.LayDown(seat, [.. cards[11..14]]);
        hand.Discard(seat, next);

        return (hand, seat);
    }

    private static void RoundTo(Hand hand, int seat)
    {
        while (hand.Phase == HandPhase.Playing && hand.TurnSeat != seat)
        {
            hand.PlayOutTheTurn(hand.TurnSeat);
        }
    }

    /// <summary>Whatever is held that was not dealt: the card just drawn.</summary>
    private static int Drawn(Hand hand, int seat, IReadOnlyList<int> dealt)
    {
        hand.Draw(seat, DrawnFrom.Stock);

        return hand.Hands[seat].Single(held => !dealt.Contains(held.Id)).Id;
    }
}
