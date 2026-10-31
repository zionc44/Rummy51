using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Authentication.Login;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// When false the cookie lasts only as long as the browser is open. When
    /// true it survives a restart, for the configured number of days.
    /// </summary>
    public bool RememberMe { get; init; }
}
