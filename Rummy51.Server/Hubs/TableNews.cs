using Microsoft.AspNetCore.SignalR;
using Rummy51.Application.Tables;

namespace Rummy51.Server.Hubs;

/// <summary>
/// What a table is told when its hand changes: the view the whole table
/// shares to the group, and then each player's own cards to that player
/// alone. Two messages, because one of the two is a secret.
///
/// It is here rather than in whoever happens to change a hand because there
/// are two of them now - a player's move and the clock running out - and
/// a table told one way by one of them and another way by the other is a
/// bug nobody would find by reading either.
/// </summary>
public sealed class TableNews(ITables tables, IHubContext<LobbyHub> lobby, ILogger<TableNews> log)
{
    /// <summary>
    /// How long one client is given to take a message before it is left
    /// without it. A browser that was closed without saying so does not
    /// refuse what is sent to it; it accepts until the pipe between here
    /// and there is full, and then it accepts nothing and says nothing,
    /// until SignalR gives up on it half a minute later.
    ///
    /// The clock behind this runs every second. Waiting on that would hand
    /// a table's beat to whichever window somebody closed this morning.
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    public async Task AboutTheHandAsync(Guid tableId)
    {
        await TellAsync(
            lobby.Clients.Group(tableId.ToString()),
            LobbyEvents.HandChanged,
            tables.Hand(tableId));

        foreach (var playerId in Seated(tableId))
        {
            var who = lobby.Clients.User(playerId.ToString());

            await TellAsync(who, LobbyEvents.CardsDealt, tables.CardsOf(tableId, playerId));
            await TellAsync(who, LobbyEvents.AboutARedeal, tables.RedealAsSeenBy(tableId, playerId));
        }
    }

    /// <summary>
    /// A message somebody may never read, and that is allowed. Every one of
    /// these carries the whole of the state rather than a change to it, so
    /// one that does not arrive costs nothing: the next one says everything
    /// the lost one would have said. What would cost something is waiting.
    /// </summary>
    private async Task TellAsync(IClientProxy who, string what, object? news)
    {
        using var patience = new CancellationTokenSource(Patience);

        try
        {
            await who.SendAsync(what, news, patience.Token);
        }
        catch (Exception failure)
        {
            log.LogWarning(failure, "A table could not be told about {What}.", what);
        }
    }

    private IEnumerable<Guid> Seated(Guid tableId) =>
        (tables.Find(tableId)?.Seats ?? []).Select(seat => seat.PlayerId).OfType<Guid>();
}
