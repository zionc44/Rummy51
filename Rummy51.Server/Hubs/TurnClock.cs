using Rummy51.Application.Tables;

namespace Rummy51.Server.Hubs;

/// <summary>
/// The first thing in this server that happens because time passed rather
/// than because somebody asked. A player who has closed the browser cannot
/// cut for themselves, so the clock has to be the server's: the ring on the
/// screen draws a deadline it was told about and decides nothing.
///
/// It sweeps rather than scheduling a timer for each table. A timer per
/// table is a timer to cancel, to reschedule and to leak, and a second is
/// finer than anybody can see on a ring that runs for eight.
/// </summary>
internal sealed class TurnClock(
    ITables tables,
    TableNews news,
    ILogger<TurnClock> log) : BackgroundService
{
    private static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        using var beat = new PeriodicTimer(Beat);

        while (await beat.WaitForNextTickAsync(stopping))
        {
            // One table that throws must not stop the clock for the rest,
            // and a clock that has stopped leaves every table waiting for
            // somebody who is never going to move.
            try
            {
                await ActForTheOverdueAsync();
            }
            catch (Exception failure)
            {
                log.LogError(failure, "The turn clock could not act for an overdue table.");
            }
        }
    }

    private async Task ActForTheOverdueAsync()
    {
        foreach (var tableId in tables.ActForTheOverdue())
        {
            await news.AboutTheHandAsync(tableId);
        }
    }
}
