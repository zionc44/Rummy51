using Rummy51.Application.Authentication.Register;
using Rummy51.Application.Common;

namespace Rummy51.Application.Authentication;

/// <summary>
/// Player accounts, described in the application's own terms. The
/// implementation lives in Infrastructure, where Identity does the work.
/// </summary>
public interface IPlayerAccountService
{
    Task<Result<RegisterPlayerResponse>> RegisterAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default);
}
