using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;

namespace Rummy51.Application.Account;

/// <summary>The bytes to send, and what identifies them.</summary>
public sealed record StoredAvatar(byte[] Image, string ContentType, string Tag);

public sealed record ChooseAvatarRequest
{
    public AvatarSource Source { get; init; }
}

public interface IPlayerAvatarService
{
    /// <summary>
    /// Re-encodes the picture and keeps the result, never what arrived, then
    /// shows it. The browser cropped it already; this is not where we find out
    /// whether it did.
    /// </summary>
    Task<Result<PlayerSummary>> UploadAsync(Stream image, CancellationToken cancellationToken = default);

    /// <summary>Drops the uploaded picture and falls back to the provider's.</summary>
    Task<Result<PlayerSummary>> RemoveAsync(CancellationToken cancellationToken = default);

    Task<Result<PlayerSummary>> ChooseAsync(ChooseAvatarRequest request, CancellationToken cancellationToken = default);

    /// <summary>What to serve for a player, or null when they uploaded nothing.</summary>
    Task<StoredAvatar?> FindAsync(Guid playerId, CancellationToken cancellationToken = default);
}
