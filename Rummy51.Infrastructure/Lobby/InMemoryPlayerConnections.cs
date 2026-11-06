using Rummy51.Application.Authentication;
using Rummy51.Application.Lobby;

namespace Rummy51.Infrastructure.Lobby;

/// <summary>
/// Memory only. Every connection dies with the process anyway, so there would
/// be nothing to restore from a store that outlived it.
/// </summary>
public sealed class InMemoryPlayerConnections : IPlayerConnections
{
    // Two views of the same fact, kept in step under one lock. Writes happen
    // once per connect and disconnect, so contention is not a concern and
    // being obviously correct is worth more than being lock-free.
    private readonly Lock gate = new();
    private readonly Dictionary<string, PlayerSummary> byConnection = [];
    private readonly Dictionary<Guid, string> byPlayer = [];

    public string? Register(PlayerSummary player, string connectionId)
    {
        lock (gate)
        {
            byPlayer.TryGetValue(player.PlayerId, out var displaced);

            if (displaced is not null)
            {
                byConnection.Remove(displaced);
            }

            byPlayer[player.PlayerId] = connectionId;
            byConnection[connectionId] = player;

            return displaced;
        }
    }

    public void Remove(string connectionId)
    {
        lock (gate)
        {
            if (!byConnection.Remove(connectionId, out var player))
            {
                return;
            }

            // A displaced connection reports its disconnect after the new one
            // has already claimed the player, so only clear the player's entry
            // while it still points here.
            if (byPlayer.TryGetValue(player.PlayerId, out var current) && current == connectionId)
            {
                byPlayer.Remove(player.PlayerId);
            }
        }
    }

    public IReadOnlyList<LobbyPlayer> Online()
    {
        lock (gate)
        {
            // Ordinal, not the current culture: the server's locale must not
            // decide what order Hebrew and Latin names appear in.
            return
            [
                .. byConnection.Values
                    .OrderBy(player => player.DisplayName, StringComparer.Ordinal)
                    // Holding a connection is all we can observe so far. In a
                    // game, and idle, arrive with the things that produce them.
                    .Select(player => new LobbyPlayer(
                        player.PlayerId,
                        player.DisplayName,
                        player.AvatarUrl,
                        PlayerStatus.Available))
            ];
        }
    }
}
