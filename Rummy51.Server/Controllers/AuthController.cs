using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;
using Rummy51.Server.Errors;

namespace Rummy51.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IPlayerAccountService accounts) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterPlayerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.RegisterAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        var (status, title) = result.Failure == FailureKind.Conflict
            ? (StatusCodes.Status409Conflict, "Conflict")
            : (StatusCodes.Status400BadRequest, "ValidationFailed");

        return ApiProblem.Result(HttpContext, status, title, result.Errors);
    }
}
