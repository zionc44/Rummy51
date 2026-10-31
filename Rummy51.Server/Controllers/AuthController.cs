using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;

namespace Rummy51.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IPlayerAccountService accounts) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterPlayerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
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

        if (result.Failure == FailureKind.Conflict)
        {
            return Problem(
                title: "Registration failed",
                detail: string.Join(" ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status409Conflict);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }
}
