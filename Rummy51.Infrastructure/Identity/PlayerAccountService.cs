using Microsoft.AspNetCore.Identity;
using Rummy51.Application.Authentication;
using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Identity;

public sealed class PlayerAccountService(
    UserManager<Player> userManager,
    TimeProvider timeProvider) : IPlayerAccountService
{
    public async Task<Result<RegisterPlayerResponse>> RegisterAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default)
    {
        var player = new Player
        {
            // Version 7 is time-ordered, which keeps the primary key index from
            // fragmenting the way random GUIDs do.
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            // Nothing sends mail yet, so an unconfirmed address could never
            // become confirmed. Revisit when there is a mail sender.
            EmailConfirmed = true,
            DisplayName = request.DisplayName,
            CreatedAt = timeProvider.GetUtcNow()
        };

        // CreateAsync hashes the password and enforces the configured rules;
        // it also reports a taken email, so no pre-check is needed.
        var created = await userManager.CreateAsync(player, request.Password);

        if (created.Succeeded)
        {
            return Result<RegisterPlayerResponse>.Success(
                new RegisterPlayerResponse(player.Id, player.Email, player.DisplayName));
        }

        // A taken address and a weak password arrive through the same channel,
        // but they are not the same answer to the caller.
        var failure = created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")
            ? FailureKind.Conflict
            : FailureKind.Validation;

        return Result<RegisterPlayerResponse>.Fail(
            failure,
            [.. created.Errors.Select(e => new Error(e.Code, e.Description))]);
    }
}
