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
        var player = NewPlayer(request);

        // CreateAsync hashes the password and enforces the configured rules;
        // it also reports a taken email, so no pre-check is needed.
        var created = await userManager.CreateAsync(player, request.Password);

        return created.Succeeded
            ? Result<PlayerSummary>.Success(Summarise(player))
            : RegistrationFailure(created.Errors);
    }

    public async Task<Result<PlayerSummary>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var player = await userManager.FindByEmailAsync(request.Email);

        if (player is null)
        {
            return InvalidCredentials;
        }

        var attempt = await signInManager.CheckPasswordSignInAsync(
            player,
            request.Password,
            lockoutOnFailure: true);

        if (!attempt.Succeeded)
        {
            return await RejectionFor(attempt, player);
        }

        await IssueCookieAsync(player, request.RememberMe);
        await RecordSignInAsync(player);

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

    private Player NewPlayer(RegisterPlayerRequest request) => new()
    {
        // Version 7 is time-ordered, which keeps the primary key index from
        // fragmenting the way random GUIDs do.
        Id = Guid.CreateVersion7(),
        UserName = request.Email,
        Email = request.Email,
        // Nothing sends mail yet, so an unconfirmed address could never become
        // confirmed. Revisit when there is a mail sender.
        EmailConfirmed = true,
        DisplayName = request.DisplayName,
        CreatedAt = timeProvider.GetUtcNow()
    };

    private Result<PlayerSummary> RegistrationFailure(IEnumerable<IdentityError> errors)
    {
        var reported = errors.ToArray();

        // A taken address and a weak password arrive through the same channel,
        // but they are not the same answer to the caller.
        var taken = reported.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");

        // UserName is a copy of Email, so one taken address is reported twice.
        var deduplicated = taken
            ? reported.Where(e => e.Code != "DuplicateUserName")
            : reported;

        return Result<PlayerSummary>.Fail(
            taken ? FailureKind.Conflict : FailureKind.Validation,
            [.. deduplicated.Select(Translate).DistinctBy(e => e.Code)]);
    }

    private async Task<Result<PlayerSummary>> RejectionFor(SignInResult attempt, Player player) =>
        attempt switch
        {
            { IsLockedOut: true } => Result<PlayerSummary>.Fail(
                FailureKind.Unauthorized,
                Error.With("AccountLocked", await MinutesLockedAsync(player))),
            { IsNotAllowed: true } => Result<PlayerSummary>.Fail(
                FailureKind.Unauthorized,
                new Error("LoginNotAllowed")),
            // An unknown address and a wrong password give the same answer.
            // Telling them apart would reveal which addresses are registered.
            _ => InvalidCredentials
        };

    /// <summary>
    /// CheckPasswordSignInAsync only verifies. Issuing the cookie separately is
    /// what lets "remember me" set its own lifetime rather than inheriting the
    /// idle timeout.
    /// </summary>
    private Task IssueCookieAsync(Player player, bool rememberMe) =>
        signInManager.SignInAsync(player, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? timeProvider.GetUtcNow().AddDays(settings.CurrentValue.RememberMeDays)
                : null
        });

    private Task RecordSignInAsync(Player player)
    {
        player.LastLoginAt = timeProvider.GetUtcNow();

        return userManager.UpdateAsync(player);
    }

    private async Task<string> MinutesLockedAsync(Player player)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(player);
        var now = timeProvider.GetUtcNow();

        // Round up, and never report zero: "locked for 0 minutes" reads as a bug.
        return Number(Math.Max(1, (int)Math.Ceiling(((lockoutEnd ?? now) - now).TotalMinutes)));
    }

    /// <summary>
    /// Identity bakes its limits into English prose ("at least 8 characters").
    /// The client is given the code and the limit separately, read back from the
    /// options that produced the error, so the two can never disagree.
    /// </summary>
    private Error Translate(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => Error.With(error.Code, Number(userManager.Options.Password.RequiredLength)),
        "PasswordRequiresUniqueChars" => Error.With(error.Code, Number(userManager.Options.Password.RequiredUniqueChars)),
        _ => new Error(error.Code)
    };

    private static Result<PlayerSummary> InvalidCredentials =>
        Result<PlayerSummary>.Fail(FailureKind.Unauthorized, new Error("InvalidCredentials"));

    private static PlayerSummary Summarise(Player player) =>
        new(player.Id, player.Email ?? string.Empty, player.DisplayName);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
