using System.Globalization;
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

        // UserName is a copy of Email, so Identity reports one taken address
        // as two separate errors. The client should see one.
        var reported = failure == FailureKind.Conflict
            ? created.Errors.Where(e => e.Code != "DuplicateUserName")
            : created.Errors;

        return Result<RegisterPlayerResponse>.Fail(
            failure,
            [.. reported.Select(Translate).DistinctBy(e => e.Code)]);
    }

    /// <summary>
    /// Identity bakes its limits into English prose ("at least 8 characters").
    /// The client is given the code and the limit separately, read back from
    /// the options that produced the error, so the two can never disagree.
    /// </summary>
    private Error Translate(IdentityError error)
    {
        var password = userManager.Options.Password;

        return error.Code switch
        {
            "PasswordTooShort" => Error.With(error.Code, Number(password.RequiredLength)),
            "PasswordRequiresUniqueChars" => Error.With(error.Code, Number(password.RequiredUniqueChars)),
            _ => new Error(error.Code)
        };

        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
