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
public sealed class TableNews(ITables tables, IHubContext<LobbyHub> lobby)
{
    public async Task AboutTheHandAsync(Guid tableId)
    {
        await lobby.Clients.Group(tableId.ToString())
            .SendAsync(LobbyEvents.HandChanged, tables.Hand(tableId));

        foreach (var playerId in Seated(tableId))
        {
            await lobby.Clients.User(playerId.ToString())
                .SendAsync(LobbyEvents.CardsDealt, tables.CardsOf(tableId, playerId));
        }
    }

    private IEnumerable<Guid> Seated(Guid tableId) =>
        (tables.Find(tableId)?.Seats ?? []).Select(seat => seat.PlayerId).OfType<Guid>();
}
