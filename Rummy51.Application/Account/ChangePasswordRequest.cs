using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Account;

/// <summary>Replacing a password always costs the current one.</summary>
public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "CurrentPasswordRequired")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    public string NewPassword { get; init; } = string.Empty;
}
