using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.Login;
using Rummy51.Application.Authentication.Register;
using Rummy51.Server.Errors;

namespace Rummy51.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IPlayerAccountService accounts) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.RegisterAsync(request, cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : this.Failure(result);
    }

    /// <summary>Signs in and issues the authentication cookie.</summary>
    [HttpPost("login")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.LoginAsync(request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await accounts.LogoutAsync();
        return NoContent();
    }

    /// <summary>
    /// The signed-in player. The cookie is encrypted, so unlike a JWT the
    /// client cannot read who it is from the credential itself.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var player = await accounts.GetCurrentPlayerAsync();

        // Authorized, yet no player: the account was removed while the cookie
        // was still valid. Clear it rather than report an empty session.
        if (player is null)
        {
            await accounts.LogoutAsync();
            return Unauthorized();
        }

        return Ok(player);
    }
}
