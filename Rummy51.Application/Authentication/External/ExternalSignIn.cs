using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Authentication.External;

public enum ExternalSignInOutcome
{
    /// <summary>The provider was already linked, or a new account was made for it.</summary>
    SignedIn,

    /// <summary>
    /// Someone registered this address with a password. Linking waits until that
    /// password proves who is asking, because nothing has ever verified that the
    /// person who registered it owns the mailbox.
    /// </summary>
    PasswordRequiredToLink,
}

public sealed record ExternalSignIn(
    ExternalSignInOutcome Outcome,
    PlayerSummary? Player,
    string? EmailAwaitingLink);

public sealed record LinkExternalLoginRequest
{
    [Required(ErrorMessage = "PasswordRequired")]
    public string Password { get; init; } = string.Empty;
}
