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
        if (departed is not null && tables.Leave(departed.PlayerId))
        {
            await BroadcastTables();
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

    private Task BroadcastTables() =>
        Clients.All.SendAsync(LobbyEvents.TablesChanged, tables.All());

    private async Task<PlayerSummary?> CurrentPlayerAsync()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var playerId)
            ? await accounts.FindPlayerAsync(playerId)
            : null;
    }
}
