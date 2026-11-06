using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Rummy51.Application.Common;

namespace Rummy51.Infrastructure.Identity;

internal static class IdentityErrors
{
    /// <summary>
    /// Identity bakes its limits into English prose ("at least 8 characters").
    /// The client is given the code and the limit separately, read back from
    /// the options that produced the error, so the two can never disagree.
    /// </summary>
    public static Error[] From(IEnumerable<IdentityError> errors, PasswordOptions password) =>
        [.. errors.Select(error => Translate(error, password)).DistinctBy(error => error.Code)];

    private static Error Translate(IdentityError error, PasswordOptions password) => error.Code switch
    {
        "PasswordTooShort" => Error.With(error.Code, Number(password.RequiredLength)),
        "PasswordRequiresUniqueChars" => Error.With(error.Code, Number(password.RequiredUniqueChars)),
        // UserName is a copy of the email, so Identity complaining about one is
        // a complaint about the other - in a vocabulary the client never learnt.
        "InvalidUserName" or "InvalidEmail" => new Error("EmailInvalid"),
        _ => new Error(error.Code)
    };

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
