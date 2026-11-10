using Rummy51.Application.Tables;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// How long a table is prepared to wait, and whether it has waited that long
/// already. It knows nothing of what is being waited for: the cut, the deal
/// and a turn are all somebody taking their time over something, and only the
/// length of the patience differs.
/// </summary>
internal sealed class Countdown(TimeProvider clock)
{
    private DateTimeOffset? dueAt;
    private TimeSpan length;

    public void Restart(TimeSpan howLong)
    {
        length = howLong;
        dueAt = clock.GetUtcNow() + howLong;
    }

    public void Stop()
    {
        dueAt = null;
        length = TimeSpan.Zero;
    }

    /// <summary>
    /// True once the wait is up. False while it is still running, and false
    /// when there is nothing being waited for at all.
    /// </summary>
    public bool HasRunOut => dueAt is { } due && clock.GetUtcNow() >= due;

    /// <summary>
    /// The wait as the table should be told it, or null when nobody is being
    /// waited for. What is left is worked out now rather than when the wait
    /// began, so whoever is told has the same deadline as everybody else
    /// however late they arrived.
    /// </summary>
    public Waiting? Describe(int seat)
    {
        if (dueAt is not { } due || seat == -1)
        {
            return null;
        }

        var left = due - clock.GetUtcNow();

        return new Waiting(seat, Math.Max(left.TotalSeconds, 0), length.TotalSeconds);
    }
}
