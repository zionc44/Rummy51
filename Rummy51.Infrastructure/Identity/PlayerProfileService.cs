using Microsoft.AspNetCore.Identity;
using Rummy51.Application.Account;
using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Identity;

internal sealed class PlayerProfileService(
    UserManager<Player> userManager,
    SignInManager<Player> signInManager) : IPlayerProfileService
{
    public async Task<Result<PlayerSummary>> ChangeDisplayNameAsync(ChangeDisplayNameRequest request)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        player.DisplayName = request.DisplayName.Trim();

        return Outcome(await userManager.UpdateAsync(player), player);
    }

    public async Task<Result<PlayerSummary>> SetPasswordAsync(SetPasswordRequest request)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        if (await userManager.HasPasswordAsync(player))
        {
            return Refused("PasswordAlreadySet");
        }

        return await ApplyAsync(
            await userManager.AddPasswordAsync(player, request.Password),
            player);
    }

    public async Task<Result<PlayerSummary>> ChangePasswordAsync(ChangePasswordRequest request)
    {
        if (await CurrentPlayerAsync() is not { } player)
        {
            return SessionExpired;
        }

        // Without a password there is nothing to prove, so this would be a way
        // to set one without the account's own consent.
        if (!await userManager.HasPasswordAsync(player))
        {
            return Refused("PasswordNotSet");
        }

        return await ApplyAsync(
            await userManager.ChangePasswordAsync(player, request.CurrentPassword, request.NewPassword),
            player);
    }

    /// <summary>
    /// Either password change moves the security stamp, which is what makes
    /// every other session stale. Reissuing the cookie keeps this browser from
    /// being signed out by its own change.
    /// </summary>
    private async Task<Result<PlayerSummary>> ApplyAsync(IdentityResult result, Player player)
    {
        if (!result.Succeeded)
        {
            return Rejected(result);
        }

        await signInManager.RefreshSignInAsync(player);

        return Summary(player);
    }

    private Result<PlayerSummary> Outcome(IdentityResult result, Player player) =>
        result.Succeeded ? Summary(player) : Rejected(result);

    private Result<PlayerSummary> Rejected(IdentityResult result) =>
        Result<PlayerSummary>.Fail(
            FailureKind.Validation,
            IdentityErrors.From(result.Errors, userManager.Options.Password));

    private Task<Player?> CurrentPlayerAsync() =>
        userManager.GetUserAsync(signInManager.Context.User);

    private static Result<PlayerSummary> Summary(Player player) =>
        Result<PlayerSummary>.Success(PlayerSignIn.Summarise(player));

    private static Result<PlayerSummary> Refused(string code) =>
        Result<PlayerSummary>.Fail(FailureKind.Conflict, new Error(code));

    /// <summary>Authorized by the cookie, yet the account behind it is gone.</summary>
    private static Result<PlayerSummary> SessionExpired =>
        Result<PlayerSummary>.Fail(FailureKind.Unauthorized, new Error("SessionExpired"));
}
