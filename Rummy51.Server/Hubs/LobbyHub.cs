using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Rummy51.Application.Authentication;
using Rummy51.Application.Lobby;
using Rummy51.Application.Tables;

namespace Rummy51.Server.Hubs;

/// <summary>
/// The lobby's live channel. For now it reports who is here; tables will be
/// announced over the same connection.
/// </summary>
[Authorize]
public sealed class LobbyHub(
    IPlayerConnections connections,
    ITables tables,
    TableNews news,
    IPlayerAccountService accounts) : Hub
{
    public const string Route = "/hubs/lobby";

    public override async Task OnConnectedAsync()
    {
        var player = await CurrentPlayerAsync();

        if (player is null)
        {
            // A cookie that outlived the account it names.
            Context.Abort();
            return;
        }

        var displaced = connections.Register(player, Context.ConnectionId);

        if (displaced is not null)
        {
            await TellItWasReplaced(displaced);
        }

        await BroadcastOnline();

        // Only the arrival needs the tables; nobody else's view changed.
        await Clients.Caller.SendAsync(LobbyEvents.TablesChanged, tables.All());

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var departed = connections.Remove(Context.ConnectionId);

        // A seat is given up only when the player has actually gone. A second
        // tab displacing the first returns null here, and displaces nobody
        // from their chair.
        // Rules 10.2, 10.3.
        if (departed is not null && tables.WentAwayFromEverything(departed.PlayerId))
        {
            await TellEverybodyAsync();
        }

        await BroadcastOnline();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Closing the old connection is the client's job, and this only asks. It
    /// does not need to be obeyed: the registry has already forgotten that
    /// connection, so it is absent from the roster and nothing is routed to it.
    /// </summary>
    private Task TellItWasReplaced(string connectionId) =>
        Clients.Client(connectionId).SendAsync(LobbyEvents.SignedInElsewhere);

    private Task BroadcastOnline() =>
        Clients.All.SendAsync(LobbyEvents.OnlinePlayersChanged, connections.Online());

    /// <summary>
    /// The table page asks to hear about one table. A group rather than a
    /// broadcast, because a hand is nobody else's business - and because the
    /// cards it will carry are about to be somebody's secret.
    /// </summary>
    public async Task<HandView?> Watch(Guid tableId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tableId.ToString());

        // Their own cards come back on the same message the deal uses, so
        // the client has one way of learning them and not two.
        if (PlayerId() is { } playerId)
        {
            // Chapter 10: arriving here is leaving wherever they were, so
            // more than this table may have changed.
            if (tables.Watching(playerId, tableId))
            {
                await TellEverybodyAsync();
            }

            await Clients.Caller.SendAsync(LobbyEvents.CardsDealt, tables.CardsOf(tableId, playerId));
            await Clients.Caller.SendAsync(
                LobbyEvents.AboutARedeal,
                tables.RedealAsSeenBy(tableId, playerId));
        }

        return tables.Hand(tableId);
    }

    /// <summary>
    /// Rule 10.2: leaving the page is leaving the table, and there is no
    /// other way to get up from a chair.
    /// </summary>
    public async Task Unwatch(Guid tableId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tableId.ToString());

        if (PlayerId() is { } playerId && tables.WentAway(playerId, tableId))
        {
            await TellEverybodyAsync();
        }
    }

    /// <summary>
    /// Chapter 10 again: who is watching what shows in two places, the rows
    /// in the lobby and the seats at a table, and a player moving between
    /// tables changes it at more than one of them. Telling every table is
    /// cheaper than working out which, and happens only when somebody moves.
    /// </summary>
    private async Task TellEverybodyAsync()
    {
        await news.AboutTheTablesAsync();

        foreach (var table in tables.All())
        {
            await news.AboutTheHandAsync(table.TableId);
        }
    }

    private Task BroadcastTables() =>
        Clients.All.SendAsync(LobbyEvents.TablesChanged, tables.All());

    private async Task<PlayerSummary?> CurrentPlayerAsync() =>
        PlayerId() is { } playerId ? await accounts.FindPlayerAsync(playerId) : null;

    private Guid? PlayerId() =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var playerId)
            ? playerId
            : null;
}
