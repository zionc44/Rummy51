using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Rummy51.Application.Account;

namespace Rummy51.Server.Controllers;

/// <summary>
/// What one player may see of another. Behind the cookie like everything
/// else: a picture is not public because it is small.
/// </summary>
[ApiController]
[Authorize]
public class PlayersController(IPlayerAvatarService avatars) : ControllerBase
{
    [HttpGet(AvatarRoute.Template)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Avatar(Guid playerId, CancellationToken cancellationToken)
    {
        var avatar = await avatars.FindAsync(playerId, cancellationToken);

        if (avatar is null)
        {
            return NotFound();
        }

        // The tag is in the address, so a different picture arrives at a
        // different address and this answer can be kept for good. Private,
        // because it is one player's face behind an authenticated route.
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";

        return File(
            avatar.Image,
            avatar.ContentType,
            lastModified: null,
            entityTag: new EntityTagHeaderValue($"\"{avatar.Tag}\""));
    }
}
