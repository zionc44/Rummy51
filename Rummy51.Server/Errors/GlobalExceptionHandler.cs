using Microsoft.AspNetCore.Diagnostics;
using Rummy51.Application.Common;

namespace Rummy51.Server.Errors;

/// <summary>
/// Catches what nothing else expected. Expected failures travel back as a
/// Result and never reach here; anything that does is a defect, so it is
/// logged in full and reported as a single opaque code.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Nothing about the exception crosses the wire: a message or stack
        // trace tells an attacker about the internals and tells the player
        // nothing. The traceId is the thread back to the log entry above.
        var problem = ApiProblem.Create(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "UnexpectedError",
            [new Error("UnexpectedError")]);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Handed over rather than set on the response, which writing the
        // body would set back to plain JSON - and leave this the one path
        // of the three answering with a type of its own.
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, ApiProblem.ProblemJson, cancellationToken);

        return true;
    }
}
