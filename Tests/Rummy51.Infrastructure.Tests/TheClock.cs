namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// Rule 3.6's instrument, asked about on its own. It knows nothing of what
/// is being waited for, so everything here is about the waiting itself.
/// </summary>
public class TheClock
{
    [Fact]
    public void A_wait_for_nothing_never_runs_out()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        clock.Advance(TimeSpan.FromHours(1));

        Assert.False(countdown.HasRunOut);
        Assert.Null(countdown.Describe(0));
    }

    [Fact]
    public void A_wait_runs_out_at_its_deadline_and_not_before()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(15));
        clock.Advance(TimeSpan.FromSeconds(14));

        Assert.False(countdown.HasRunOut);

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.True(countdown.HasRunOut);
    }

    [Fact]
    public void Stopping_one_leaves_nothing_being_waited_for()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(5));
        countdown.Stop();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(countdown.HasRunOut);
        Assert.Null(countdown.Describe(0));
    }

    [Fact]
    public void Restarting_one_moves_the_deadline_rather_than_adding_to_it()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(30));
        clock.Advance(TimeSpan.FromSeconds(29));
        countdown.Restart(TimeSpan.FromSeconds(5));

        Assert.False(countdown.HasRunOut);
        Assert.Equal(5, countdown.Describe(0)!.SecondsLeft);
    }

    /// <summary>
    /// What is left is worked out when it is asked for, which is what makes
    /// a player who arrives late share everybody else's deadline rather
    /// than getting a fresh one of their own.
    /// </summary>
    [Fact]
    public void What_is_left_is_counted_from_now_and_not_from_the_start()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(20));
        clock.Advance(TimeSpan.FromSeconds(12));

        var waiting = countdown.Describe(2)!;

        Assert.Equal(8, waiting.SecondsLeft);
        Assert.Equal(20, waiting.Seconds);
    }

    [Fact]
    public void And_never_less_than_nothing_however_late_it_is_asked()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(10));
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(0, countdown.Describe(1)!.SecondsLeft);
    }

    /// <summary>
    /// A seat of -1 is the table waiting rather than any one player, which
    /// is what the deal and the three stages of a redeal are.
    /// </summary>
    [Fact]
    public void The_seat_it_is_told_is_the_seat_it_reports()
    {
        var clock = new FakeTimeProvider();
        var countdown = new Countdown(clock);

        countdown.Restart(TimeSpan.FromSeconds(10));

        Assert.Equal(2, countdown.Describe(2)!.Seat);
        Assert.Equal(-1, countdown.Describe(-1)!.Seat);
    }
}
