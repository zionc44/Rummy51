using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Account;

/// <summary>The same rules the sign-up form answers to, so one name is as
/// valid on the second day as it was on the first.</summary>
public sealed record ChangeDisplayNameRequest
{
    [Required(ErrorMessage = "DisplayNameRequired")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "DisplayNameLength")]
    public string DisplayName { get; init; } = string.Empty;
}
