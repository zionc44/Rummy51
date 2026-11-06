namespace Rummy51.Application.Lobby;

/// <summary>
/// What the lobby shows about someone. Only <see cref="PlayerStatus.Available"/>
/// is reachable today: the rest need games to exist, and an idle rule to be
/// agreed. The vocabulary is here because the display already depends on it.
/// </summary>
public enum PlayerStatus
{
    Available,
    Idle,
    InGame,
    InGameIdle,
}

public sealed record LobbyPlayer(
    Guid PlayerId,
    string DisplayName,
    string? AvatarUrl,
    PlayerStatus Status);
