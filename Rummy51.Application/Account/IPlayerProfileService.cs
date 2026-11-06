using Rummy51.Application.Authentication;
using Rummy51.Application.Common;

namespace Rummy51.Application.Account;

/// <summary>
/// What a signed-in player may change about themselves. Each call answers with
/// the player as they now are, so the client never has to ask again.
/// </summary>
public interface IPlayerProfileService
{
    Task<Result<PlayerSummary>> ChangeDisplayNameAsync(ChangeDisplayNameRequest request);

    /// <summary>Gives a password to an account that has none.</summary>
    Task<Result<PlayerSummary>> SetPasswordAsync(SetPasswordRequest request);

    /// <summary>Replaces an existing password, proven by the current one.</summary>
    Task<Result<PlayerSummary>> ChangePasswordAsync(ChangePasswordRequest request);
}
