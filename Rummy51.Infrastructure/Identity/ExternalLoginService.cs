using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.External;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Identity;

internal sealed class ExternalLoginService(
    UserManager<Player> userManager,
    SignInManager<Player> signInManager,
    PlayerSignIn signIn,
    TimeProvider timeProvider) : IExternalLoginService
{
    public async Task<Result<ExternalSignIn>> CompleteSignInAsync()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();

        if (info is null)
        {
            return Unavailable<ExternalSignIn>();
        }

        if (EmailOf(info) is not { } email)
        {
            return Failed<ExternalSignIn>("ExternalLoginWithoutEmail");
        }

        var linked = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (linked is not null)
        {
            return await SignedInAsync(linked, info);
        }

        var existing = await userManager.FindByEmailAsync(email);

        return existing is null
            ? await CreateFromProviderAsync(info, email)
            : AwaitingPassword(email);
    }

    public async Task<Result<PlayerSummary>> LinkToExistingAccountAsync(LinkExternalLoginRequest request)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();

        if (info is null)
        {
            return Unavailable<PlayerSummary>();
        }

        if (EmailOf(info) is not { } email ||
            await userManager.FindByEmailAsync(email) is not { } player)
        {
            return InvalidCredentials;
        }

        // Counts towards lockout like any other password attempt, so this
        // endpoint cannot be the cheap way around the limit on the other one.
        var attempt = await signInManager.CheckPasswordSignInAsync(player, request.Password, true);

        if (!attempt.Succeeded)
        {
            return InvalidCredentials;
        }

        var linked = await userManager.AddLoginAsync(player, info);

        if (!linked.Succeeded)
        {
            return Result<PlayerSummary>.Fail(
                FailureKind.Validation,
                [.. linked.Errors.Select(e => new Error(e.Code)).DistinctBy(e => e.Code)]);
        }

        await AdoptAvatarAsync(player, info);
        await signIn.IssueAsync(player, rememberMe: true);

        return Result<PlayerSummary>.Success(PlayerSignIn.Summarise(player));
    }

    private async Task<Result<ExternalSignIn>> CreateFromProviderAsync(ExternalLoginInfo info, string email)
    {
        var player = new Player
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            // The provider verified the mailbox before telling us about it.
            EmailConfirmed = true,
            DisplayName = DisplayNameOf(info, email),
            AvatarUrl = ClaimOf(info, "picture"),
            CreatedAt = timeProvider.GetUtcNow()
        };

        var created = await userManager.CreateAsync(player);

        if (!created.Succeeded)
        {
            return Result<ExternalSignIn>.Fail(
                FailureKind.Validation,
                [.. created.Errors.Select(e => new Error(e.Code)).DistinctBy(e => e.Code)]);
        }

        await userManager.AddLoginAsync(player, info);

        return await SignedInAsync(player, info);
    }

    private async Task<Result<ExternalSignIn>> SignedInAsync(Player player, ExternalLoginInfo info)
    {
        await AdoptAvatarAsync(player, info);
        await signIn.IssueAsync(player, rememberMe: true);

        return Result<ExternalSignIn>.Success(new ExternalSignIn(
            ExternalSignInOutcome.SignedIn,
            PlayerSignIn.Summarise(player),
            null));
    }

    /// <summary>
    /// Keeps the picture current, and writes only when it actually changed —
    /// this runs on every sign-in.
    /// </summary>
    private async Task AdoptAvatarAsync(Player player, ExternalLoginInfo info)
    {
        var picture = ClaimOf(info, "picture");

        if (picture is null || picture == player.AvatarUrl)
        {
            return;
        }

        player.AvatarUrl = picture;
        await userManager.UpdateAsync(player);
    }

    private static string DisplayNameOf(ExternalLoginInfo info, string email) =>
        ClaimOf(info, ClaimTypes.GivenName)
        ?? ClaimOf(info, ClaimTypes.Name)
        // Better a readable stand-in than the whole address on the table.
        ?? email.Split('@')[0];

    private static string? EmailOf(ExternalLoginInfo info)
    {
        var email = ClaimOf(info, ClaimTypes.Email);

        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    private static string? ClaimOf(ExternalLoginInfo info, string type) =>
        info.Principal.FindFirstValue(type);

    private static Result<PlayerSummary> InvalidCredentials =>
        Result<PlayerSummary>.Fail(FailureKind.Unauthorized, new Error("InvalidCredentials"));

    /// <summary>The cookie holding the provider's answer expired, or never arrived.</summary>
    private static Result<T> Unavailable<T>() => Failed<T>("ExternalLoginUnavailable");

    private static Result<T> Failed<T>(string code) =>
        Result<T>.Fail(FailureKind.Unauthorized, new Error(code));

    private static Result<ExternalSignIn> AwaitingPassword(string email) =>
        Result<ExternalSignIn>.Success(new ExternalSignIn(
            ExternalSignInOutcome.PasswordRequiredToLink,
            null,
            email));
}
