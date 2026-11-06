using Rummy51.Application.Common;

namespace Rummy51.Application.Authentication.External;

/// <summary>
/// Signing in through someone else's identity provider. Between the provider
/// answering and the player finishing, the identity it returned waits in a
/// short-lived cookie that only the server can read — so neither call takes it
/// as an argument, and the browser cannot claim to be someone it is not.
/// </summary>
public interface IExternalLoginService
{
    /// <summary>Picks up where the provider left off.</summary>
    Task<Result<ExternalSignIn>> CompleteSignInAsync();

    /// <summary>
    /// Attaches the waiting identity to the account that already owns its email.
    /// </summary>
    Task<Result<PlayerSummary>> LinkToExistingAccountAsync(LinkExternalLoginRequest request);
}
