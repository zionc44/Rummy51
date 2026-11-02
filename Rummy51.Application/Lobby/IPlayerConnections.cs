using Rummy51.Application.Authentication;

namespace Rummy51.Application.Lobby;

/// <summary>
/// Who is currently in the lobby. A player holds at most one connection: a
/// second one displaces the first, which keeps "online" unambiguous and gives
/// the lobby and the tables a single source of truth.
/// </summary>
public interface IPlayerConnections
{
    /// <summary>
    /// Records the connection as the player's only one.
    /// </summary>
    /// <returns>
    /// The connection this one displaced, or null if the player was not already
    /// connected. The caller is expected to tell that connection why it is
    /// going away.
    /// </returns>
    string? Register(PlayerSummary player, string connectionId);

    void Remove(string connectionId);

    IReadOnlyList<PlayerSummary> Online();
}
