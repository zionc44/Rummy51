namespace Rummy51.Application.Authentication;

/// <summary>What the client is told about a player. Never includes a secret.</summary>
public sealed record PlayerSummary(
    Guid PlayerId,
    string Email,
    string DisplayName,
    string? AvatarUrl);
