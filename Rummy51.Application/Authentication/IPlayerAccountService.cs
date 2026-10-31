using Rummy51.Application.Authentication.Login;
using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;

namespace Rummy51.Application.Authentication;

/// <summary>
/// Player accounts, described in the application's own terms. The
/// implementation lives in Infrastructure, where Identity does the work.
/// </summary>
public interface IPlayerAccountService
{
    Task<Result<PlayerSummary>> RegisterAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Signs the player in, which issues the authentication cookie.</summary>
    Task<Result<PlayerSummary>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task LogoutAsync();

    /// <summary>The signed-in player, or null when there is no live session.</summary>
    Task<PlayerSummary?> GetCurrentPlayerAsync();
}
