using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rummy51.Application.Common;

namespace Rummy51.Server.Errors;

/// <summary>
/// Turns the framework's model-state complaints into the project's error codes.
/// </summary>
internal static class ModelStateErrors
{
    private const string Malformed = "MalformedRequest";

    public static IReadOnlyList<Error> From(ModelStateDictionary modelState) =>
    [
        .. modelState
            .SelectMany(entry => entry.Value?.Errors ?? [])
            .Select(error => AsCode(error.ErrorMessage))
            .Distinct(StringComparer.Ordinal)
            .Select(code => new Error(code))
    ];

    /// <summary>
    /// Every rule in a request type carries a code as its ErrorMessage. Anything
    /// else is framework prose — a JSON parser's complaint, say — which must not
    /// reach the client dressed as a code it could look up.
    /// </summary>
    private static string AsCode(string message) =>
        message.Length > 0 && message.All(char.IsAsciiLetterOrDigit) ? message : Malformed;
}
