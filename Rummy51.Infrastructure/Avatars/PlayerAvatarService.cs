using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rummy51.Application.Account;
using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;
using Rummy51.Infrastructure.Identity;
using Rummy51.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Rummy51.Infrastructure.Avatars;

internal sealed class PlayerAvatarService(
    Rummy51DbContext database,
    UserManager<Player> userManager,
    SignInManager<Player> signInManager,
    TimeProvider timeProvider) : IPlayerAvatarService
{
    /// <summary>
    /// The circle is drawn at fifty pixels at most, but a dense screen asks
    /// for three times that, and the table will want it larger still.
    /// </summary>
    private const int Side = 256;

    /// <summary>
    /// Refused before decoding. A small file can still describe an enormous
    /// picture, and the memory to decode one is the picture's size, not the
    /// file's.
    /// </summary>
    private const int LargestAccepted = 8000;

    private const string Webp = "image/webp";

    public async Task<Result<PlayerSummary>> UploadAsync(
        Stream image,
        CancellationToken cancellationToken = default)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        var encoded = await SquareWebpAsync(image, cancellationToken);

        if (encoded is null)
        {
            return Refused("AvatarNotAnImage");
        }

        await StoreAsync(player.Id, encoded, cancellationToken);

        player.AvatarImageTag = TagOf(encoded);
        player.AvatarSource = AvatarSource.Uploaded;

        return await SaveAsync(player);
    }

    public async Task<Result<PlayerSummary>> RemoveAsync(CancellationToken cancellationToken = default)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        await database.PlayerAvatars
            .Where(avatar => avatar.PlayerId == player.Id)
            .ExecuteDeleteAsync(cancellationToken);

        player.AvatarImageTag = null;
        player.AvatarSource = AvatarSource.Provider;

        return await SaveAsync(player);
    }

    public async Task<Result<PlayerSummary>> ChooseAsync(
        ChooseAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        if (request.Source == AvatarSource.Uploaded && player.AvatarImageTag is null)
        {
            return Refused("NoUploadedAvatar");
        }

        player.AvatarSource = request.Source;

        return await SaveAsync(player);
    }

    public async Task<StoredAvatar?> FindAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var avatar = await database.PlayerAvatars
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PlayerId == playerId, cancellationToken);

        return avatar is null
            ? null
            : new StoredAvatar(avatar.Image, avatar.ContentType, TagOf(avatar.Image));
    }

    /// <summary>
    /// Decodes the picture and draws a square one from its pixels. Nothing of
    /// the original file survives the trip, which is the point: the metadata
    /// goes with it, and the GPS coordinates a phone writes into it go too.
    /// </summary>
    private static async Task<byte[]?> SquareWebpAsync(Stream image, CancellationToken cancellationToken)
    {
        try
        {
            using var buffered = new MemoryStream();
            await image.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;

            // The header alone says how big this will be once unpacked.
            var described = await Image.IdentifyAsync(buffered, cancellationToken);

            if (described.Width > LargestAccepted || described.Height > LargestAccepted)
            {
                return null;
            }

            buffered.Position = 0;
            using var decoded = await Image.LoadAsync(buffered, cancellationToken);

            decoded.Mutate(picture => picture.Resize(new ResizeOptions
            {
                Size = new Size(Side, Side),
                // Centres and trims whatever is not already square. The player
                // has usually cropped it themselves; this is the floor, not
                // the feature.
                Mode = ResizeMode.Crop
            }));

            using var output = new MemoryStream();
            await decoded.SaveAsWebpAsync(output, cancellationToken);

            return output.ToArray();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Anything unreadable is an answer to the player, not a fault.
            return null;
        }
    }

    /// <summary>
    /// The picture is written before the row that points at it, so the tag
    /// never names bytes that are not there yet. A failure between the two
    /// leaves a picture nothing refers to, which costs a few kilobytes and
    /// nothing else.
    /// </summary>
    private async Task StoreAsync(Guid playerId, byte[] image, CancellationToken cancellationToken)
    {
        var existing = await database.PlayerAvatars
            .FirstOrDefaultAsync(avatar => avatar.PlayerId == playerId, cancellationToken);

        if (existing is null)
        {
            database.PlayerAvatars.Add(new PlayerAvatar
            {
                PlayerId = playerId,
                Image = image,
                ContentType = Webp,
                UpdatedAt = timeProvider.GetUtcNow()
            });
        }
        else
        {
            existing.Image = image;
            existing.ContentType = Webp;
            existing.UpdatedAt = timeProvider.GetUtcNow();
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<PlayerSummary>> SaveAsync(Player player)
    {
        var saved = await userManager.UpdateAsync(player);

        return saved.Succeeded
            ? Result<PlayerSummary>.Success(PlayerSignIn.Summarise(player))
            : Result<PlayerSummary>.Fail(
                FailureKind.Validation,
                IdentityErrors.From(saved.Errors, userManager.Options.Password));
    }

    private Task<Player?> CurrentPlayerAsync() =>
        userManager.GetUserAsync(signInManager.Context.User);

    /// <summary>Short, and derived from the bytes, so the same picture is always
    /// the same address and a different one never is.</summary>
    private static string TagOf(byte[] image) =>
        Convert.ToHexString(SHA256.HashData(image))[..32].ToLowerInvariant();

    private static Result<PlayerSummary> Refused(string code) =>
        Result<PlayerSummary>.Fail(FailureKind.Validation, new Error(code));

    private static Result<PlayerSummary> SessionExpired =>
        Result<PlayerSummary>.Fail(FailureKind.Unauthorized, new Error("SessionExpired"));
}
