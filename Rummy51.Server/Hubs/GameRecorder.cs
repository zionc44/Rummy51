using Rummy51.Application.Games;
using Rummy51.Application.Tables;

namespace Rummy51.Server.Hubs;

/// <summary>
/// Takes what the tables have finished with and writes it down. It is its
/// own sweep rather than a line in <see cref="TurnClock"/> because they
/// answer to different things: that one must not miss a beat, and a result
/// written a second late is a result written.
///
/// A failure here loses a result and not a game. That is the trade on
/// purpose - a table that stopped because a database did would be the
/// worse of the two - and it is why this says so loudly in the log.
/// </summary>
internal sealed class GameRecorder(
    ITables tables,
    IGameRecords records,
    ILogger<GameRecorder> log) : BackgroundService
{
    private static readonly TimeSpan Beat = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        using var beat = new PeriodicTimer(Beat);

        while (await beat.WaitForNextTickAsync(stopping))
        {
            var taken = tables.TakeRecords();

            try
            {
                await records.WriteAsync(taken, stopping);
            }
            catch (Exception failure)
            {
                // Taken already, so they are gone. Saying what they were is
                // the only thing left that helps anybody.
                log.LogError(failure, "{Count} game records could not be written: {Records}",
                    taken.Count, taken);
            }
        }
    }
}
