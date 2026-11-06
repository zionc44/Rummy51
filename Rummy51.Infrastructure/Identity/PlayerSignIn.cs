using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Rummy51.Application.Authentication;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Identity;

/// <summary>
/// Everything that happens once a player has proved who they are, whichever
/// way they proved it. Three callers reach this point: a password sign-in, a
/// provider sign-in, and linking a provider to an existing account.
/// </summary>
internal sealed class PlayerSignIn(
    SignInManager<Player> signInManager,
    UserManager<Player> userManager,
    IOptionsMonitor<AuthenticationSettings> settings,
    TimeProvider timeProvider)
{
    public async Task IssueAsync(Player player, bool rememberMe)
    {
        await IssueCookieAsync(player, rememberMe);

        player.LastLoginAt = timeProvider.GetUtcNow();
        await userManager.UpdateAsync(player);
    }

    public static PlayerSummary Summarise(Player player) =>
        new(
            player.Id,
            player.Email ?? string.Empty,
            player.DisplayName,
            player.AvatarUrl,
            // The test UserManager.HasPasswordAsync makes, made here so that
            // summarising a player does not have to become asynchronous.
            player.PasswordHash is not null);

    /// <summary>
    /// Verifying a password and issuing a cookie are deliberately separate:
    /// one call would do both, but then "remember me" could not set its own
    /// lifetime instead of inheriting the idle timeout.
    /// </summary>
    private Task IssueCookieAsync(Player player, bool rememberMe) =>
        signInManager.SignInAsync(player, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? timeProvider.GetUtcNow().AddDays(settings.CurrentValue.RememberMeDays)
                : null
        });
}
