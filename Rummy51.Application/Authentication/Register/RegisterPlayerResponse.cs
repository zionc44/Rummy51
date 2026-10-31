namespace Rummy51.Application.Authentication.Register;

public sealed record RegisterPlayerResponse(Guid PlayerId, string Email, string DisplayName);
