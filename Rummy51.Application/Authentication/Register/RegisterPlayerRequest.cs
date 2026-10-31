using System.ComponentModel.DataAnnotations;

namespace Rummy51.Application.Authentication.Register;

public sealed record RegisterPlayerRequest
{
    // The ErrorMessage of each rule is the code the client translates, not text
    // meant to be read. Codes name the field as well as the rule, so most of
    // them carry no parameters and the client can phrase them directly.
    //
    // The properties are not declared `required`: System.Text.Json rejects a
    // payload missing such a property before validation runs, and its complaint
    // is English prose rather than a code. Defaulting to empty lets the
    // Required rule answer instead.

    /// <summary>Doubles as the login identifier, so it has to be unique.</summary>
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    [MaxLength(256, ErrorMessage = "EmailTooLong")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Checked against the Identity password rules, not here.</summary>
    [Required(ErrorMessage = "PasswordRequired")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "DisplayNameRequired")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "DisplayNameLength")]
    public string DisplayName { get; init; } = string.Empty;
}
