using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rummy51.Application.Account;
using Rummy51.Application.Authentication;
using Rummy51.Server.Errors;

namespace Rummy51.Server.Controllers;

/// <summary>
/// What a player changes about themselves. Every route here acts on whoever
/// the cookie names, so none of them takes an id: an account cannot be edited
/// by asking for someone else's.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountController(
    IPlayerProfileService profile,
    IPlayerAvatarService avatars) : ControllerBase
{
    /// <summary>A cropped square is a few kilobytes. This is room to be wrong,
    /// not room to send a photograph straight off a phone.</summary>
    private const int LargestUpload = 2 * 1024 * 1024;

    [HttpPut("display-name")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeDisplayName(ChangeDisplayNameRequest request)
    {
        var result = await profile.ChangeDisplayNameAsync(request);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    /// <summary>
    /// POST rather than PUT: this creates a password where there was none, and
    /// is refused on an account that already has one.
    /// </summary>
    [HttpPost("password")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request)
    {
        var result = await profile.SetPasswordAsync(request);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    /// <summary>
    /// The browser crops before sending, which keeps the upload small. It is
    /// a convenience and not a control: anything can be posted here, so the
    /// picture is decoded and drawn again whatever arrives.
    /// </summary>
    [HttpPost("avatar")]
    [RequestSizeLimit(LargestUpload)]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile image, CancellationToken cancellationToken)
    {
        await using var content = image.OpenReadStream();

        var result = await avatars.UploadAsync(content, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpDelete("avatar")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveAvatar(CancellationToken cancellationToken)
    {
        var result = await avatars.RemoveAsync(cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    /// <summary>Which of the two pictures to show. Both are kept either way.</summary>
    [HttpPut("avatar/source")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChooseAvatar(ChooseAvatarRequest request, CancellationToken cancellationToken)
    {
        var result = await avatars.ChooseAsync(request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }

    [HttpPut("password")]
    [ProducesResponseType<PlayerSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var result = await profile.ChangePasswordAsync(request);

        return result.IsSuccess ? Ok(result.Value) : this.Failure(result);
    }
}
