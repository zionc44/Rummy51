using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.Login;
using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Identity;

public sealed class PlayerAccountService(
    UserManager<Player> userManager,
    SignInManager<Player> signInManager,
    IOptionsMonitor<AuthenticationSettings> settings,
    TimeProvider timeProvider) : IPlayerAccountService
{
    public async Task<Result<PlayerSummary>> RegisterAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default)
    {
        var player = new Player
        {
            // Version 7 is time-ordered, which keeps the primary key index from
            // fragmenting the way random GUIDs do.
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            // Nothing sends mail yet, so an unconfirmed address could never
            // become confirmed. Revisit when there is a mail sender.
            EmailConfirmed = true,
            DisplayName = request.DisplayName,
            CreatedAt = timeProvider.GetUtcNow()
        };

        // CreateAsync hashes the password and enforces the configured rules;
        // it also reports a taken email, so no pre-check is needed.
        var created = await userManager.CreateAsync(player, request.Password);

        if (created.Succeeded)
        {
            return Result<PlayerSummary>.Success(Summarise(player));
        }

        // A taken address and a weak password arrive through the same channel,
        // but they are not the same answer to the caller.
        var failure = created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")
            ? FailureKind.Conflict
            : FailureKind.Validation;

        // UserName is a copy of Email, so Identity reports one taken address
        // as two separate errors. The client should see one.
        var reported = failure == FailureKind.Conflict
            ? created.Errors.Where(e => e.Code != "DuplicateUserName")
            : created.Errors;

        return Result<PlayerSummary>.Fail(
            failure,
            [.. reported.Select(Translate).DistinctBy(e => e.Code)]);
    }

    public async Task<Result<PlayerSummary>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var player = await userManager.FindByEmailAsync(request.Email);

        // An unknown address and a wrong password give the same answer. Telling
        // them apart would let anyone test which addresses are registered here.
        if (player is null)
        {
            return InvalidCredentials();
        }

        var attempt = await signInManager.CheckPasswordSignInAsync(
            player,
            request.Password,
            lockoutOnFailure: true);

        if (attempt.IsLockedOut)
        {
            return Result<PlayerSummary>.Fail(
                FailureKind.Unauthorized,
                Error.With("AccountLocked", await MinutesLockedAsync(player)));
        }

        if (attempt.IsNotAllowed)
        {
            return Result<PlayerSummary>.Fail(
                FailureKind.Unauthorized,
                new Error("LoginNotAllowed"));
        }

        if (!attempt.Succeeded)
        {
            return InvalidCredentials();
        }

        var now = timeProvider.GetUtcNow();

        // CheckPasswordSignInAsync only verifies; this is what issues the
        // cookie, and doing it separately is what lets "remember me" set its
        // own lifetime instead of inheriting the idle timeout.
        await signInManager.SignInAsync(player, new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = request.RememberMe
                ? now.AddDays(settings.CurrentValue.RememberMeDays)
                : null
        });

        player.LastLoginAt = now;
        await userManager.UpdateAsync(player);

        return Result<PlayerSummary>.Success(Summarise(player));
    }

    public Task LogoutAsync() => signInManager.SignOutAsync();

    public async Task<PlayerSummary?> GetCurrentPlayerAsync()
    {
        var principal = signInManager.Context.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Read back rather than trust the cookie's claims: the display name may
        // have changed, and the account may have been removed since sign-in.
        var player = await userManager.GetUserAsync(principal);

        return player is null ? null : Summarise(player);
    }

    private static Result<PlayerSummary> InvalidCredentials() =>
        Result<PlayerSummary>.Fail(FailureKind.Unauthorized, new Error("InvalidCredentials"));

    private static PlayerSummary Summarise(Player player) =>
        new(player.Id, player.Email ?? string.Empty, player.DisplayName);

    private async Task<string> MinutesLockedAsync(Player player)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(player);
        var remaining = (lockoutEnd ?? timeProvider.GetUtcNow()) - timeProvider.GetUtcNow();

        // Round up, and never report zero: "locked for 0 minutes" reads as a bug.
        return Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))
            .ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Identity bakes its limits into English prose ("at least 8 characters").
    /// The client is given the code and the limit separately, read back from
    /// the options that produced the error, so the two can never disagree.
    /// </summary>
    private Error Translate(IdentityError error)
    {
        var password = userManager.Options.Password;

        return error.Code switch
        {
            "PasswordTooShort" => Error.With(error.Code, Number(password.RequiredLength)),
            "PasswordRequiresUniqueChars" => Error.With(error.Code, Number(password.RequiredUniqueChars)),
            _ => new Error(error.Code)
        };

        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
