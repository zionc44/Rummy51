using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.External;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;
using Rummy51.Server.Errors;

namespace Rummy51.Server.Controllers;

/// <summary>
/// Signing in through Google. The first two steps are browser navigations and
/// answer with redirects; only the last is a call the client makes itself.
/// Every destination below is a relative path, so the player stays on the
/// origin they started from.
/// </summary>
[ApiController]
[Route("api/auth/google")]
public class GoogleAuthController(
    IExternalLoginService external,
    SignInManager<Player> signInManager) : ControllerBase
{
    [HttpGet("start")]
    public IActionResult Start()
    {
        // Identity builds the properties rather than this method, because it
        // stamps the provider name into the items that travel through the
        // round trip and refuses to read the answer back without it.
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme,
            Url.Action(nameof(Callback)));

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var result = await external.CompleteSignInAsync();

        if (!result.IsSuccess)
        {
            return Redirect($"/login?error={result.Errors[0].Code}");
        }

        return result.Value! switch
        {
            { Outcome: ExternalSignInOutcome.SignedIn } => Redirect("/lobby"),
            { EmailAwaitingLink: { } email } => Redirect(LinkPageFor(email)),
            _ => Redirect("/login?error=UnexpectedError")
        };
    }

    /// <summary>
    /// Proves ownership of an account that already holds this email, and only
    /// then attaches the Google identity waiting in the external cookie.
    /// </summary>
    [HttpPost("link")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Link(LinkExternalLoginRequest request)
    {
        var result = await external.LinkToExistingAccountAsync(request);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var status = result.Failure == FailureKind.Unauthorized
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status400BadRequest;

        return ApiProblem.Result(HttpContext, status, "Unauthorized", result.Errors);
    }

    private static string LinkPageFor(string email) =>
        $"/link-google?email={Uri.EscapeDataString(email)}";
}
