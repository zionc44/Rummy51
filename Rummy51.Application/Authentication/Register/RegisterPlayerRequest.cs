using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Authentication.Register;

public sealed record RegisterPlayerRequest
{
    /// <summary>Doubles as the login identifier, so it has to be unique.</summary>
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    /// <summary>Validated against the Identity password rules, not here.</summary>
    [Required]
    public required string Password { get; init; }

    [Required, StringLength(50, MinimumLength = 2)]
    public required string DisplayName { get; init; }
}
