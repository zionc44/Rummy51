using Rummy51.Application.Authentication.Login;
using Rummy51.Application.Authentication.Register;
using Rummy51.Domain.Entities;

namespace Rummy51.Server.Tests;

/// <summary>
/// The account service as a dial rather than an implementation. Each test
/// sets what the next call answers, because what is being asked is never
/// what the service decided - it is what the pipeline made of the answer.
/// </summary>
public sealed class Accounts : IPlayerAccountService
{
    private Result<PlayerSummary> answer = Result<PlayerSummary>.Success(Somebody);

    public Exception? Throwing { get; set; }

    public PlayerSummary? Current { get; set; }

    public static PlayerSummary Somebody { get; } = new(
        Guid.NewGuid(), "somebody@example.test", "Somebody", null, null, null, AvatarSource.Provider, true);

    public void Succeeding() => answer = Result<PlayerSummary>.Success(Somebody);

    public void Refusing(FailureKind kind, params string[] codes) =>
        answer = Result<PlayerSummary>.Fail(kind, [.. codes.Select(code => new Error(code))]);

    public void Refusing(FailureKind kind, Error error) =>
        answer = Result<PlayerSummary>.Fail(kind, error);

    public Task<Result<PlayerSummary>> RegisterAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default) => Answer();

    public Task<Result<PlayerSummary>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) => Answer();

    public Task LogoutAsync() => Task.CompletedTask;

    public Task<PlayerSummary?> GetCurrentPlayerAsync() => Task.FromResult(Current);

    public Task<PlayerSummary?> FindPlayerAsync(Guid playerId) => Task.FromResult(Current);

    private Task<Result<PlayerSummary>> Answer() =>
        Throwing is null ? Task.FromResult(answer) : Task.FromException<Result<PlayerSummary>>(Throwing);
}
