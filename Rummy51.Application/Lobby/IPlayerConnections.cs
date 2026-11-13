using Rummy51.Application.Authentication;

namespace Rummy51.Application.Lobby;

/// <summary>
/// Who is currently in the lobby. A player holds at most one connection: a
/// second one displaces the first, which keeps "online" unambiguous and gives
/// the lobby and the tables a single source of truth.
/// </summary>
public interface IPlayerConnections
{
    /// <returns>
    /// The connection this one displaced, which the caller is expected to
    /// tell why it is going away. Null if the player was not connected.
    /// </returns>
    string? Register(PlayerSummary player, string connectionId);

    /// <returns>
    /// The player, when this was the connection they were holding. Null when
    /// a newer one had already replaced it, in which case they have not gone
    /// anywhere and nothing of theirs should be given up.
    /// </returns>
    PlayerSummary? Remove(string connectionId);

    IReadOnlyList<LobbyPlayer> Online();
}
