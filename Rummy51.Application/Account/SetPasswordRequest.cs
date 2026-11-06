using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Account;

/// <summary>
/// The first password on an account that has none, which is every account
/// created through a provider. There is no current password to ask for.
/// </summary>
public sealed record SetPasswordRequest
{
    [Required(ErrorMessage = "PasswordRequired")]
    public string Password { get; init; } = string.Empty;
}
