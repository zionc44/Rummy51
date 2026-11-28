namespace Rummy51.Domain.Tests;

/// <summary>
/// Rules 8.4 and 8.5: what a series makes of each of its hands, before any
/// of them is played and as redeals are agreed in them.
/// </summary>
public class TheSeries
{
    [Theory]
    [InlineData(3, new[] { 1, 1, 2 })]
    [InlineData(4, new[] { 1, 2, 1, 4 })]
    [InlineData(6, new[] { 1, 1, 2, 1, 1, 4 })]
    [InlineData(8, new[] { 1, 2, 1, 4, 1, 2, 1, 4 })]
    [InlineData(12, new[] { 1, 1, 2, 1, 1, 4, 1, 1, 2, 1, 1, 4 })]
    public void Rule_8_4_some_hands_count_double_or_four_times_from_the_start(int hands, int[] fixedAt)
    {
        var weights = Series.Weights(hands, []);

        Assert.Equal(fixedAt, weights.Select(weight => weight.Fixed));
        Assert.DoesNotContain(weights, weight => weight.Redealt);
    }

    [Fact]
    public void Rule_8_5_a_redeal_in_a_hand_counting_single_doubles_that_hand()
    {
        var weights = Series.Weights(4, [1]);

        Assert.True(weights[0].Redealt);
        Assert.Equal(2, weights[0].Times);
    }

    [Fact]
    public void Rule_8_5_a_redeal_in_a_hand_already_doubled_falls_on_the_next_single_one()
    {
        var weights = Series.Weights(4, [0, 1]);

        Assert.False(weights[1].Redealt);
        Assert.True(weights[2].Redealt);
    }

    [Fact]
    public void Rule_8_5_and_a_hand_a_redeal_fell_on_passes_the_next_one_on()
    {
        var weights = Series.Weights(6, [2]);

        Assert.Equal([true, true, false, false, false, false], weights.Select(weight => weight.Redealt));
    }

    [Fact]
    public void Rule_8_5_a_redeal_that_finds_no_single_hand_left_is_lost()
    {
        var weights = Series.Weights(3, [0, 0, 1]);

        Assert.DoesNotContain(weights, weight => weight.Redealt);
    }

    /// <summary>Rule 8.5's own worked example.</summary>
    [Fact]
    public void Rule_8_5_twelve_redeals_in_the_first_of_twelve_hands_double_every_hand()
    {
        var weights = Series.Weights(12, [12]);

        Assert.All(weights, weight => Assert.True(weight.Times >= 2));
        Assert.Equal(8, weights.Count(weight => weight.Redealt));
        Assert.DoesNotContain(weights, weight => weight.Redealt && weight.Fixed > 1);
    }
}
