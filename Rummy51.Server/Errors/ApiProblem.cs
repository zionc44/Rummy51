using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Common;

namespace Rummy51.Server.Errors;

/// <summary>
/// Builds the one response shape every failure uses, whether it was expected
/// (a Result carrying errors) or not (an unhandled exception). The client gets
/// a single payload to parse and a single place to look up wording.
/// </summary>
internal static class ApiProblem
{
    private const string ProblemJson = "application/problem+json";

    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string title,
        IReadOnlyList<Error> errors)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            // Title and type are for whoever reads the response by hand. What
            // the user is shown comes from the codes below.
            Title = title,
            Instance = $"{context.Request.Method} {context.Request.Path}"
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["errors"] = errors;

        return problem;
    }

    public static ObjectResult Result(
        HttpContext context,
        int status,
        string title,
        IReadOnlyList<Error> errors)
    {
        var result = new ObjectResult(Create(context, status, title, errors)) { StatusCode = status };
        result.ContentTypes.Add(ProblemJson);
        return result;
    }
}
