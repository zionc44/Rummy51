using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Common;

namespace Rummy51.Server.Errors;

internal static class FailedResult
{
    /// <summary>
    /// Turns the reason an operation failed into the status code that says the
    /// same thing over HTTP. Three controllers decided this separately before
    /// it moved here, which is three chances for them to disagree.
    /// </summary>
    public static IActionResult Failure<T>(this ControllerBase controller, Result<T> result)
    {
        var (status, title) = result.Failure switch
        {
            FailureKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            FailureKind.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status400BadRequest, "ValidationFailed")
        };

        return ApiProblem.Result(controller.HttpContext, status, title, result.Errors);
    }
}
