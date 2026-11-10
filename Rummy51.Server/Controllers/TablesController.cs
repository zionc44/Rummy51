using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Application.Tables;
using Rummy51.Server.Errors;
using Rummy51.Server.Hubs;

namespace Rummy51.Server.Controllers;

/// <summary>
/// Opening, joining and leaving a table. The answer goes to whoever asked;
/// everyone else hears about it over the lobby's connection, because a seat
/// changing hands changes what every other player is looking at.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TablesController(
    ITables tables,
    IPlayerAccountService accounts,
    IHubContext<LobbyHub> lobby,
    TableNews news) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LobbyTable>>(StatusCodes.Status200OK)]
    public IActionResult All() => Ok(tables.All());

    [HttpGet("{tableId:guid}")]
    [ProducesResponseType<LobbyTable>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult One(Guid tableId)
    {
        var table = tables.Find(tableId);

        return table is null ? NotFound() : Ok(table);
    }

    [HttpPost]
    [ProducesResponseType<LobbyTable>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open(CreateTableRequest request)
    {
        if (await accounts.GetCurrentPlayerAsync() is not { } player)
        {
            return Unauthorized();
        }

        var result = tables.Open(request, player);

        if (!result.IsSuccess)
        {
            return this.Failure(result);
        }

        await AnnounceAsync();

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("{tableId:guid}/join")]
    [ProducesResponseType<LobbyTable>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(Guid tableId)
    {
        if (await accounts.GetCurrentPlayerAsync() is not { } player)
        {
            return Unauthorized();
        }

        var result = tables.Join(tableId, player);

        if (!result.IsSuccess)
        {
            return this.Failure(result);
        }

        await AnnounceAsync();

        // Taking the last seat starts the hand, and whoever is already
        // watching this table has to be told.
        await TellTheTableAsync(tableId);

        return Ok(result.Value);
    }

    /// <summary>
    /// The cutter chose where to cut. The card there becomes the finishing
    /// card, and the one after it is the first that will be dealt.
    /// </summary>
    [HttpPost("{tableId:guid}/cut")]
    [ProducesResponseType<HandView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Cut(Guid tableId, CutRequest request) =>
        MoveAsync(tableId, playerId => tables.CutDeck(tableId, playerId, request.Position));

    /// <summary>
    /// The first half of a turn: a card off the stock, or the one the player
    /// before threw away.
    /// </summary>
    [HttpPost("{tableId:guid}/draw")]
    [ProducesResponseType<HandView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Draw(Guid tableId, DrawRequest request) =>
        MoveAsync(tableId, playerId => tables.Draw(tableId, playerId, request.From));

    /// <summary>The second half, which ends the turn and hands it on.</summary>
    [HttpPost("{tableId:guid}/discard")]
    [ProducesResponseType<HandView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Discard(Guid tableId, DiscardRequest request) =>
        MoveAsync(tableId, playerId => tables.Discard(tableId, playerId, request.CardId));

    /// <summary>
    /// Every move at a table is the same errand: find out who is asking, put
    /// it to the table, and tell the table what came of it. Only the middle
    /// of those three differs between them.
    /// </summary>
    private async Task<IActionResult> MoveAsync(Guid tableId, Func<Guid, Result<HandView>> move)
    {
        if (await accounts.GetCurrentPlayerAsync() is not { } player)
        {
            return Unauthorized();
        }

        var result = move(player.PlayerId);

        if (!result.IsSuccess)
        {
            return this.Failure(result);
        }

        await TellTheTableAsync(tableId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Leaves whichever table the player is at, which is at most one, so it
    /// needs no id. Leaving a table they are not at is not a failure.
    /// </summary>
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Leave()
    {
        if (await accounts.GetCurrentPlayerAsync() is not { } player)
        {
            return Unauthorized();
        }

        if (tables.Leave(player.PlayerId))
        {
            await AnnounceAsync();
        }

        return NoContent();
    }

    private Task AnnounceAsync() =>
        lobby.Clients.All.SendAsync(LobbyEvents.TablesChanged, tables.All());

    private Task TellTheTableAsync(Guid tableId) => news.AboutTheHandAsync(tableId);
}
