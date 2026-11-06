using Rummy51.Domain.Entities;

namespace Rummy51.Application.Authentication;

/// <summary>What the client is told about a player. Never includes a secret.</summary>
/// <param name="AvatarUrl">
/// The picture to show, already chosen between the two below so that nobody
/// else has to choose again.
/// </param>
public sealed record PlayerSummary(
    Guid PlayerId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? ProviderAvatarUrl,
    string? UploadedAvatarUrl,
    AvatarSource AvatarSource,
    bool HasPassword);
