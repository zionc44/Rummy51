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
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
